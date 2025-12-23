using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

using dTITAN.Backend.EventBus;
using dTITAN.Backend.Events;
using dTITAN.Backend.Models;

namespace dTITAN.Backend.Services;

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

                await ReceiveLoopAsync(ws, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
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

                // Preserve full exception details at Debug level (no stack trace in main logs).
                _logger.LogDebug(ex, "Full exception for drone {DroneId} on connect attempt {Attempt}", _droneId, attempt);

                await Task.Delay(delay, ct);
            }
        }
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
                    return;

                ms.Write(segment.Array!, segment.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            var payload = Encoding.UTF8.GetString(ms.ToArray());

            // XXX: This might slow down loop, if so offload to another task
            try
            {
                _logger.LogDebug("Received payload from drone {DroneId}", _droneId);
                Drone? drone = JsonSerializer.Deserialize<Drone>(payload);
                if (drone == null) continue;

                _eventBus.Publish(new DroneTelemetryReceived(drone));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize drone message for background write");
            }
        }
    }
}
