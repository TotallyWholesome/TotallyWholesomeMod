using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MelonLoader;
using MelonLoader.Logging;
using TotallyWholesome.Managers.Shockers.OpenShock.Models.SignalR;
using TotallyWholesome.Utils;
#pragma warning disable CS4014

namespace TotallyWholesome.Managers.Shockers.OpenShock;

public sealed class OpenShockSignalRWebSocket : IAsyncDisposable
{
    private readonly Uri _hubUri;
    private readonly string _token;
    private static readonly MelonLogger.Instance Logger = new("TotallyWholesome.OpenShock.SignalRClient", ColorARGB.Green);
    
    public event MessageEvent OnMessage;
    public event StatusUpdate OnStatusUpdate;
    
    private static readonly Handshake Json1Handshake = new()
    {
        Version = 1,
        Protocol = "json"
    };

    private static readonly SignalRMessage Ping = new()
    {
        Type = MessageType.Ping
    };

    private readonly Timer _timer;
    
    private readonly CancellationTokenSource _dispose;
    private CancellationTokenSource _linked;
    
    public OpenShockSignalRWebSocket(Uri hubUri, string token)
    {
        _hubUri = hubUri;
        _token = token;
        
        _dispose = new CancellationTokenSource();
        _linked = _dispose;
        
        _timer = new Timer(PingTimer, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
    }
    
    private ClientWebSocket _webSocket = null!;
    private CancellationTokenSource _currentConnectionClose = null!;

    private void PingTimer(object state)
    {
        if(_webSocket is not { State: WebSocketState.Open }) return;
        // Fire and forget is okay here, we are only adding to channel
        QueueMessage(Ping);
    }

    private Channel<object> _channel = Channel.CreateUnbounded<object>();
    
    private ValueTask QueueMessageObject(object data) => _channel.Writer.WriteAsync(data, _dispose.Token);
    public ValueTask QueueMessage(SignalRMessage message) => QueueMessageObject(message);

    private SignalRStatus _signalRStatus = SignalRStatus.Uninitialized;
    public SignalRStatus Status
    {
        get => _signalRStatus;
        private set
        {
            _signalRStatus = value;
            OnStatusUpdate?.Invoke(value);
        }
    }

    private async Task MessageLoop()
    {
        try
        {
            await foreach (var msg in _channel.Reader.ReadAllAsync(_linked.Token))
                await SignalRWebSocketUtils.SendFullMessage(
                    msg, _webSocket, _linked.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Logger.Error("Error in message loop " + e);
        }
    }

    public Task Initialize() => ConnectAsync();

    private async Task ConnectAsync()
    {
        if (_dispose.IsCancellationRequested)
        {
            Logger.Msg("Dispose requested, not connecting");
            return;
        }
        
        Status = SignalRStatus.Connecting;
        _currentConnectionClose?.Cancel();
        _currentConnectionClose = new CancellationTokenSource();
        _linked = CancellationTokenSource.CreateLinkedTokenSource(_dispose.Token, _currentConnectionClose.Token);
        
        _webSocket?.Abort();
        _webSocket?.Dispose();
        
        _channel = Channel.CreateUnbounded<object>();
        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("OpenShockToken", _token);
        _webSocket.Options.SetRequestHeader("User-Agent",
            $"TotallyWholesome/{BuildInfo.TWVersion} (OpenShockSignalRWebSocket; OpenShock Integration)");
        Logger.Msg("Connecting to websocket....");
        try
        {
            await _webSocket.ConnectAsync(_hubUri, _linked.Token);
            Logger.Msg("Connected to websocket");
            Status = SignalRStatus.Connected;
            await QueueMessageObject(Json1Handshake);
            
            TwTask.Run(ReceiveLoop, _linked.Token);
            TwTask.Run(MessageLoop, _linked.Token);
        }
        catch (Exception e)
        {
            Logger.Error("Error while connecting, retrying in 3 seconds" + e);
            Status = SignalRStatus.Reconnecting;
            _webSocket.Abort();
            _webSocket.Dispose();
            await Task.Delay(3000, _dispose.Token);
            TwTask.Run(ConnectAsync, _dispose.Token);
        }
    }

    private async Task ReceiveLoop()
    {
        while (!_linked.Token.IsCancellationRequested)
        {
            try
            {
                if (_webSocket!.State == WebSocketState.Aborted)
                {
                    Logger.Warning("Websocket connection aborted, closing loop");
                    break;
                }
                var message =
                    await SignalRWebSocketUtils.ReceiveFullMessageAsyncNonAlloc<SignalRServerMessage>(_webSocket, _linked.Token);

                if (message.IsT2)
                {
                    if (_webSocket.State != WebSocketState.Open)
                    {
                        Logger.Warning("Client sent closure, but connection state is not open");
                        break;
                    }

                    try
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal close",
                            _linked.Token);
                    }
                    catch (OperationCanceledException e)
                    {
                        Logger.Error("Error during close handshake", e);
                    }

                    Logger.Msg("Closing websocket connection");
                    break;
                }

                message.Switch(wsRequest =>
                    {
                        foreach (var res in wsRequest)
                        {
                            if(res.Type == MessageType.Close) _currentConnectionClose.Cancel();
                            if(res.Type == MessageType.Invocation) TwTask.Run(OnMessage?.Invoke(res), _dispose.Token);
                        }
                    },
                    failed => { Logger.Warning("Deserialization failed for websocket message", failed.Exception); },
                    _ => { });
            }
            catch (OperationCanceledException)
            {
                Logger.Msg("WebSocket connection terminated due to close or shutdown");
                break;
            }
            catch (WebSocketException e)
            {
                if (e.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely)
                    Logger.Error("Error in receive loop, websocket exception", e);
            }
            catch (Exception ex)
            {
                Logger.Error("Exception while processing websocket request", ex);
            }
        }

        _currentConnectionClose.Cancel();

        if (_dispose.IsCancellationRequested)
        {
            Logger.Msg("Dispose requested, not reconnecting");
            return;
        }
        
        Logger.Warning("Lost websocket connection, trying to reconnect in 3 seconds");
        Status = SignalRStatus.Reconnecting;
        
        _webSocket.Abort();
        _webSocket.Dispose();
        
        await Task.Delay(3000, _dispose.Token);

        TwTask.Run(ConnectAsync, _dispose.Token);
    }

    private bool _disposed;
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        Status = SignalRStatus.Uninitialized;
        _dispose?.Cancel();
        if (_timer != null) await _timer.DisposeAsync();
        _webSocket?.Dispose();
        GC.SuppressFinalize(this);
    }
    
    ~OpenShockSignalRWebSocket()
    {
        DisposeAsync().AsTask().Wait();
    }
}

public delegate Task MessageEvent(SignalRMessage message);
public delegate void StatusUpdate(SignalRStatus status);