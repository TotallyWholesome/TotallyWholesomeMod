#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Harmony;
using MelonLoader;
using Newtonsoft.Json;
using TotallyWholesome.Managers.Lead;
using TotallyWholesome.Managers.Shockers.OpenShock.Config;
using TotallyWholesome.Managers.Shockers.OpenShock.Models;
using TotallyWholesome.Managers.Shockers.OpenShock.Models.SignalR;
using TotallyWholesome.Managers.Status;
using TotallyWholesome.Managers.TWUI;
using TotallyWholesome.Managers.TWUI.Pages.Shocker;
using TotallyWholesome.Utils;
using TWNetCommon.Data.ControlPackets.Shockers.Models;
using Random = UnityEngine.Random;

namespace TotallyWholesome.Managers.Shockers.OpenShock;

public sealed class OpenShockManager : IShockerProvider, IAsyncDisposable
{
    public static OpenShockManager? Instance { get; private set; }
    
    private static readonly MelonLogger.Instance Logger = new("TotallyWholesome.OpenShock", Color.Green);

    private const string AuthTokenHeaderName = "OpenShockToken";

    // When the service is successfully setup
    private bool _serviceValid = false;
    
    private OpenShockSignalRWebSocket? _webSocket = null;
    private HttpClient? _httpClient = null;

    public OpenShockManager()
    {
        Instance = this;
        Logger.Msg("Starting OpenShockManager");
        TwTask.Run(SetupServiceConnection);
    }

    private void WebSocketOnStatusUpdate(SignalRStatus status)
    {
        TWMenu.Instance.OpenShockStatus = status;
    }

    private async Task WebSocketOnMessage(SignalRMessage message)
    {
        switch (message.Target)
        {
            case "DeviceUpdate":
                await UpdateOwnShockers(Guid.Parse((string)message.Arguments![0]));
                break;
        }
    }

    public void SetupServiceConnectionFnf() => TwTask.Run(SetupServiceConnection);
    

    /// <summary>
    /// Setup service connections
    /// Call this if there is a change to the service configuration
    /// </summary>
    public async Task SetupServiceConnection()
    {
        _serviceValid = false;
        _httpClient?.Dispose();
        if (_webSocket != null)
        {
            try
            {
                _webSocket.OnMessage -= WebSocketOnMessage;
                _webSocket.OnStatusUpdate -= WebSocketOnStatusUpdate;
            }
            catch (Exception e)
            {
                Logger.Warning("Failed to remove event handlers from websocket, error: " + e);
            }

            await _webSocket.DisposeAsync();
        }
            
        if (string.IsNullOrEmpty(OpenShockConfig.Config.ApiToken))
        {
            Logger.Warning("No OpenShock API token configured, please configure it to continue");
            return;
        }
        
        _httpClient = new HttpClient
        {
            BaseAddress = OpenShockConfig.Config.ApiBaseUrl
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            $"TotallyWholesome/{BuildInfo.TWVersion} (OpenShockHttpClient; OpenShock Integration)");
        _httpClient.DefaultRequestHeaders.Add(AuthTokenHeaderName, OpenShockConfig.Config.ApiToken);
        
        _webSocket = new OpenShockSignalRWebSocket(GetWebsocketUrl(), OpenShockConfig.Config.ApiToken);
        _webSocket.OnMessage += WebSocketOnMessage;
        _webSocket.OnStatusUpdate += WebSocketOnStatusUpdate;
        _serviceValid = true;
        
        await Task.WhenAll(_webSocket.Initialize(), UpdateOwnShockers());
    }

    public async Task UpdateOwnShockers(Guid? deviceId = null)
    {
        if (!_serviceValid)
        {
            Logger.Warning("OpenShock service is not connected, cannot retrieve shockers");
            return;
        }
        
        var request = new HttpRequestMessage(HttpMethod.Get, "/1/shockers/own");
        var response = await _httpClient!.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            Logger.Error("Failed to update shockers, failed with status code: " + response.StatusCode);
            return;
        }

        var json = await response.Content.ReadAsStringAsync();
        if(string.IsNullOrEmpty(json))
        {
            Logger.Error("Error parsing shockers, response was empty");
            return;
        }
        var devices = JsonConvert.DeserializeObject<BaseResponse<IReadOnlyCollection<ResponseDeviceWithShockers>>>(json)!.Data!;

        if (deviceId != null)
            if (devices.All(x => x.Id != deviceId))
            {
                Logger.Msg("Skipped device update, device not in our devices");
                return;
            }

        var oldConf = OpenShockConfig.Config.Shockers;

        var newConf = new Dictionary<Guid, OpenShockConfig.OpenShockConf.ShockerConfig>();

        foreach (var device in devices)
        {
            foreach (var shocker in device.Shockers)
            {
                if (oldConf.TryGetValue(shocker.Id, out var previousShockerConf))
                {
                    newConf[shocker.Id] = new OpenShockConfig.OpenShockConf.ShockerConfig
                    {
                        Enabled = previousShockerConf.Enabled,
                        LimitIntensity = previousShockerConf.LimitIntensity,
                        LimitDuration = previousShockerConf.LimitDuration,
                        AllowShock = previousShockerConf.AllowShock,
                        AllowVibrate = previousShockerConf.AllowVibrate,
                        AllowSound = previousShockerConf.AllowSound
                    };
                }
                else newConf[shocker.Id] = new OpenShockConfig.OpenShockConf.ShockerConfig();
            }
        }

        OpenShockConfig.Config.Shockers = newConf;

        Main.Instance.MainThreadQueue.Enqueue(() => OpenShockPage.UpdateDevices(devices));
        StatusManager.Instance.DeviceChangeStatusUpdate();
    }

    private static Uri GetWebsocketUrl()
    {
        var baseUrl = OpenShockConfig.Config.ApiBaseUrl;
        var scheme = baseUrl.Scheme == "https" ? "wss" : "ws";

        return new Uri($"{scheme}://{baseUrl.Host}/1/hubs/user");
    }

    /// <inheritdoc />
    public async Task Control(ControlType type, byte intensity, ushort duration)
    {
        if (!_serviceValid)
        {
            Logger.Warning("OpenShock service is not connected, cannot send control command");
            return;
        }
        IReadOnlyCollection<Control> controlList;

        // If below 1 intensity or 1ms duration, we send a stop command
        if (intensity < 1 || duration < 1)
        {
            controlList = ControlListAllShockersStop();
        }
        else
        {
            controlList = ConfigManager.Instance.IsActiveCurrent(AccessType.ShockRandomShocker)
                ? ControlListRandomShocker(type, intensity, duration)
                : ControlListAllShockers(type, intensity, duration);
        }

        if (controlList.Count <= 0)
        {
            return;
        }
        
        await _webSocket!.QueueMessage(new SignalRMessage
        {
            Type = MessageType.Invocation,
            Target = "ControlV2",
            Arguments =
            [
                controlList,
                LeadManager.Instance?.MasterPair?.Master?.Username
            ]
        });
    }

    private static IReadOnlyCollection<Control> ControlListRandomShocker(ControlType type, byte intensity, ushort duration)
    {
        var allEnabledShockers = OpenShockConfig.Config.Shockers.Where(x => x.Value.Enabled).ToArray();

        if (allEnabledShockers.Length <= 0)
        {
            return Array.Empty<Control>();
        }
        
        var rand = new System.Random();
        var randomElementIndex = rand.Next(0, allEnabledShockers.Length);
        var randomElement = allEnabledShockers[randomElementIndex];

        return
        [
            GetControlItem(randomElement.Key, type, intensity, duration, randomElement.Value)
        ];
    }

    private static IReadOnlyCollection<Control> ControlListAllShockersStop()
    {
        var controlList = new List<Control>();

        foreach (var (key, value) in OpenShockConfig.Config.Shockers)
        {
            if (!value.Enabled) continue;
            controlList.Add(new Control
            {
                Id = key,
                Intensity = 0,
                Duration = 0,
                Type = ControlType.Stop
            });
        }

        return controlList;
    }

    private static IReadOnlyCollection<Control> ControlListAllShockers(ControlType type, byte intensity, ushort duration)
    {
        var controlList = new List<Control>();

        foreach (var (key, value) in OpenShockConfig.Config.Shockers)
        {
            if (!value.Enabled) continue;
            controlList.Add(GetControlItem(key, type, intensity, duration, value));
        }

        return controlList;
    }

    private static Control GetControlItem(Guid key, ControlType type, byte intensity, ushort duration, OpenShockConfig.OpenShockConf.ShockerConfig value) =>
        new Control
        {
            Id = key,
            Intensity = Convert.ToByte(intensity / 100f * value.LimitIntensity),
            Duration = Math.Clamp(duration, (ushort)300, value.LimitDuration),
            Type = type
        };
    

    /// <inheritdoc />
    public bool NoLimits
    {
        get
        {
            if (OpenShockConfig.Config.Shockers.Count <= 0) return true;
            return OpenShockConfig.Config.Shockers.Values.All(x =>
                x.LimitDuration == 15_000 && x.LimitIntensity == 100);
        }
    }

    private bool _disposed = false;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        Logger.Msg("Disposing OpenShockManager");
        _httpClient?.Dispose();
        if (_webSocket != null) await _webSocket.DisposeAsync();
    }
}