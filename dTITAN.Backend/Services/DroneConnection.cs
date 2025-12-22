using System.Net.WebSockets;
using System.Text;
using dTITAN.Backend.DTO;

namespace dTITAN.Backend.Services;

/// <summary>
/// Manages a single WebSocket connection to a remote drone endpoint.
/// Responsible for connecting, reconnecting with backoff, reading messages and
/// enqueueing received payloads into the <see cref="DroneMessageQueue"/>.
/// </summary>
/// <param name="droneId">Unique identifier for the drone.</param>
/// <param name="baseUri">Base URI of the drone WebSocket endpoint.</param>
/// <param name="queue">Queue used to publish incoming drone envelopes.</param>
/// <param name="logger">Logger instance for connection diagnostics.</param>
public sealed class DroneConnection(
    string droneId,
    Uri baseUri,
    DroneMessageQueue queue,
    ILogger<DroneConnection> logger)
{
    private readonly string _droneId = droneId;
    private readonly Uri _baseUri = baseUri;
    private readonly DroneMessageQueue _queue = queue;
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

    /// <summary>
    /// Connects to the WebSocket and continuously receives messages until the
    /// socket closes or the <paramref name="ct"/> is cancelled. Received payloads
    /// are enqueued into the provided <see cref="DroneMessageQueue"/>.
    /// </summary>
    /// <param name="ws">Active client web socket instance.</param>
    /// <param name="ct">Cancellation token used to stop reception.</param>
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

            await _queue.EnqueueAsync(
                new DroneEnvelope(_droneId, payload),
                ct
            );
        }
    }
}
