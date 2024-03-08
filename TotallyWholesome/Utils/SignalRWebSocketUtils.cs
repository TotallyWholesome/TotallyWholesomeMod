#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using Microsoft.IO;
using Newtonsoft.Json;
using OneOf;


namespace TotallyWholesome.Utils;

public static class SignalRWebSocketUtils
{
    private const uint MaxMessageSize = 512_000; // 512 000 bytes
    
    // ReSharper disable once InconsistentNaming
    private const char RS = (char)0x1E;
    private static readonly byte[] RsByteArray = [0x1E];
    private static readonly RecyclableMemoryStreamManager RecyclableMemory = new();

    public static async Task<OneOf<IEnumerable<T?>, DeserializeFailed, WebsocketClosure>> ReceiveFullMessageAsyncNonAlloc<T>(
        WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            ValueWebSocketReceiveResult result;
            await using var message = RecyclableMemory.GetStream();
            var bytes = 0;
            do
            {
                result = await socket.ReceiveAsync(new Memory<byte>(buffer), cancellationToken);
                bytes += result.Count;
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closure during message read",
                        cancellationToken);
                    return new WebsocketClosure();
                }

                if (buffer.Length + result.Count > MaxMessageSize) throw new MessageTooLongException();
                message.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            try
            {
                var stringResponse = Encoding.UTF8.GetString(message.GetBuffer(), 0, bytes);
                var splitted = stringResponse.Split(RS, StringSplitOptions.RemoveEmptyEntries);
                return OneOf<IEnumerable<T?>, DeserializeFailed, WebsocketClosure>.FromT0(splitted.Select(JsonConvert.DeserializeObject<T>));
            }
            catch (Exception e)
            {
                return new DeserializeFailed { Exception = e };
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static Task SendFullMessage<T>(T obj, WebSocket socket, CancellationToken cancelToken) =>
        SendFullMessage(JsonConvert.SerializeObject(obj, SerializerOptions.CamelCaseSettings), socket, cancelToken);


    public static Task SendFullMessage(string json, WebSocket socket, CancellationToken cancelToken) =>
        SendFullMessageBytes(Encoding.UTF8.GetBytes(json), socket, cancelToken);

    public static async Task SendFullMessageBytes(byte[] msg, WebSocket socket, CancellationToken cancelToken, int maxChunkSize = 256)
    {
        var doneBytes = 0;

        while (doneBytes < msg.Length)
        {
            var bytesProcessing = Math.Min(maxChunkSize, msg.Length - doneBytes);
            var buffer = msg.AsMemory(doneBytes, bytesProcessing);

            doneBytes += bytesProcessing;
            await socket.SendAsync(buffer, WebSocketMessageType.Text, false, cancelToken);
            if (doneBytes >= msg.Length)
                await socket.SendAsync(RsByteArray, WebSocketMessageType.Text, true, cancelToken);
        }
    }
}


/// <summary>
/// When json deserialization fails
/// </summary>
public struct DeserializeFailed
{
    public Exception Exception { get; set; }
}

/// <summary>
/// When the websocket sent a close frame
/// </summary>
public readonly struct WebsocketClosure
{
}