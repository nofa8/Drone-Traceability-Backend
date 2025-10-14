using System;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using System.Text.Json;
using dTITAN.Backend.Models;

namespace dTITAN.Backend.Services;

public class ExternalWebSocketService : BackgroundService
{
    private readonly Uri _externalUri;
    private readonly DroneMessageQueue _queue;

    public ExternalWebSocketService(IConfiguration config, DroneMessageQueue queue)
    {
        var connectionString = config.GetConnectionString("DroneWS");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Drone Websocket Server connection string not configured.");
        _externalUri = new Uri(connectionString);
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ListenLoopAsync(stoppingToken);
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var ws = new ClientWebSocket();
                Console.WriteLine($"Connecting to {_externalUri}");

                await ws.ConnectAsync(_externalUri, ct);
                Console.WriteLine("Connected to external WebSocket");

                var buffer = new byte[4096];

                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("External server closed the connection");
                        break;
                    }

                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    try
                    {
                        var drone = JsonSerializer.Deserialize<Drone>(msg, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (drone != null)
                        {
                            Console.WriteLine($"Drone at {drone.Latitude},{drone.Longitude} with {drone.BatteryLevel}% battery");
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.Error.WriteLine($"Failed to parse drone JSON: {ex}");
                    }

                    // Publish into the message queue for background batch processing.
                    try
                    {
                        // Respect cancellation when enqueuing
                        await _queue.EnqueueAsync(msg, ct);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        // shutting down
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to enqueue message to DroneMessageQueue: {ex}");
                    }
                }

                    await CloseGracefully(ws);
            }
            catch (Exception ex)
            {
                    Console.Error.WriteLine($"Error in external WebSocket listener: {ex.Message}");
                    await Task.Delay(5000, ct);
            }
        }
    }

    private static async Task CloseGracefully(ClientWebSocket ws)
    {
        if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
    }

    // BackgroundService base class handles stopping via the CancellationToken passed to ExecuteAsync.
    public override void Dispose()
    {
        Console.WriteLine("ExternalWebSocketService disposed");
        base.Dispose();
    }
}
