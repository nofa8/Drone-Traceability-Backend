using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using dTITAN.Backend.Data.DTO;
using dTITAN.Backend.Data.Events;
using dTITAN.Backend.EventBus;

namespace dTITAN.Backend.Services.Ingestion;

public sealed class DroneConnection(
    string droneId,
    Uri baseUri,
    IDroneEventBus eventBus,
    ILogger<DroneConnection> logger)
{
    private readonly string _droneId = droneId;
    private readonly Uri _baseUri = baseUri;
    private readonly IDroneEventBus _eventBus = eventBus;
    private readonly ILogger<DroneConnection> _logger = logger;

    public async Task RunAsync(CancellationToken ct)
    {
        var uri = new Uri($"{_baseUri}?dboidsID={_droneId}");
        int attempt = 0;
        while (!ct.IsCancellationRequested)
        {
            using var ws = new ClientWebSocket();

            try
            {
                attempt++;
                _logger.LogInformation("Connecting to drone \"{DroneId}\"", _droneId);
                await ws.ConnectAsync(uri, ct);
                attempt = 0;
                _logger.LogInformation("Drone \"{DroneId}\" connected", _droneId);
                
                // Receive initial snapshot
                var initialPayload = await ReceiveSingleAsync(ws, ct);
                var drone = JsonSerializer.Deserialize<DroneTelemetry>(initialPayload);
                if (drone == null) throw new InvalidOperationException("Failed to parse initial drone snapshot");

                _eventBus.Publish(
                    new DroneConnected(drone, DateTime.UtcNow)
                );

                await ReceiveLoopAsync(ws, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _eventBus.Publish(new DroneDisconnected(_droneId, DateTime.UtcNow));
                break;
            }
            catch (Exception ex)
            {
                int delay = Math.Min(5000 * attempt, 20000); // max 20s

                var shortError = ex.InnerException is not null
                    ? $"{ex.GetType().Name}: {ex.InnerException.Message}"
                    : $"{ex.GetType().Name}: {ex.Message}";

                _logger.LogWarning("[{DroneId}] Connection error: {Error}. Attempt {Attempt}. Reconnecting in {Delay}ms.",
                    _droneId, shortError, attempt, delay);

                _eventBus.Publish(new DroneDisconnected(_droneId, DateTime.UtcNow));

                // Preserve full exception details at Debug level (no stack trace in main logs).
                _logger.LogDebug(ex, "Full exception for drone {DroneId} on connect attempt {Attempt}", _droneId, attempt);

                await Task.Delay(delay, ct);
            }
        }
    }


    private static async Task<string> ReceiveSingleAsync(
        ClientWebSocket ws,
        CancellationToken ct)
    {
        var buffer = new byte[4096];
        var segment = new ArraySegment<byte>(buffer);

        using var ms = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await ws.ReceiveAsync(segment, ct);

            if (result.MessageType == WebSocketMessageType.Close)
                throw new WebSocketException("Connection closed during handshake");

            ms.Write(segment.Array!, segment.Offset, result.Count);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(ms.ToArray());
    }



    private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[4096];
        var segment = new ArraySegment<byte>(buffer);

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await ws.ReceiveAsync(segment, ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _eventBus.Publish(new DroneDisconnected(_droneId, DateTime.UtcNow));
                    return;
                }

                ms.Write(segment.Array!, segment.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            var payload = Encoding.UTF8.GetString(ms.ToArray());

            // XXX: This might slow down loop, if so offload to another task
            try
            {
                _logger.LogDebug("Received payload from drone {DroneId}", _droneId);
                DroneTelemetry? drone = JsonSerializer.Deserialize<DroneTelemetry>(payload);
                if (drone == null) continue;

                _eventBus.Publish(new DroneTelemetryReceived(drone, DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize drone message for background write");
            }
        }
    }
}
