using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using dTITAN.Backend.Data.Transport.Websockets;

namespace dTITAN.Backend.Services.Ingestion;

public sealed class DroneWebSocketClient : BackgroundService
{
    private readonly ILogger _logger;
    private readonly DroneManager _manager;
    private readonly Uri _uri;

    private readonly Channel<string> _messageChannel = Channel.CreateUnbounded<string>();

    public DroneWebSocketClient(
        IConfiguration config,
        DroneManager manager,
        ILogger<DroneWebSocketClient> logger)
    {
        _logger = logger;
        _manager = manager;

        string? connectionString = config.GetConnectionString("DroneWS");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Drone WebSocket not configured.");

        // Append dboidsID=0 so we get broast data for all drones, and command acess
        _uri = new Uri(connectionString + "?dboidsID=0");
        _logger.LogInformation("Initializing WebSocketService with {_uri}", _uri);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var backoff = TimeSpan.FromSeconds(1);
        var maxBackoff = TimeSpan.FromSeconds(30);

        while (!ct.IsCancellationRequested)
        {
            using var ws = new ClientWebSocket();
            ws.Options.KeepAliveTimeout = TimeSpan.FromSeconds(10);

            try
            {
                _logger.LogInformation("Connecting to WebSocket {Uri}", _uri);
                await ws.ConnectAsync(_uri, ct);
                backoff = TimeSpan.FromSeconds(1);

                // start background processor
                Task processTask = ProcessMessages(ct);

                await ReceiveLoop(ws, ct);

                // complete channel so processor finishes
                _messageChannel.Writer.Complete();
                await processTask;
            }
            catch (OperationCanceledException)
            {
                break; // service shutting down
            }
            catch (Exception ex) when (ex is WebSocketException || ex is IOException)
            {
                _logger.LogWarning(ex, "WebSocket connection lost");
            }

            if (ct.IsCancellationRequested) break;

            _logger.LogInformation("Reconnecting in {Delay}s", backoff.TotalSeconds);
            await Task.Delay(backoff, ct);
            backoff = TimeSpan.FromSeconds(
                Math.Min(backoff.TotalSeconds * 2, maxBackoff.TotalSeconds)
            );
        }
    }

    private async Task ReceiveLoop(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new ArraySegment<byte>(new byte[8192]);
        while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("Remote requested close of WebSocket");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", ct);
                    return;
                }
                ms.Write(buffer.Array!, buffer.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            if (ms.Length == 0) continue;
            var text = Encoding.UTF8.GetString(ms.ToArray());
            await _messageChannel.Writer.WriteAsync(text, ct);
        }
    }

    private async Task ProcessMessages(CancellationToken ct)
    {
        await foreach (var payload in _messageChannel.Reader.ReadAllAsync(ct))
        {
            ExternalEnvelopeWs? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<ExternalEnvelopeWs>(payload);
                if (envelope == null) continue;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid WS envelope");
                continue;
            }

            if (envelope.Role == "drone") HandleTelemetry(envelope);
            else _logger.LogWarning("Unknown role {Role}", envelope.Role);
        }
    }

    private void HandleTelemetry(ExternalEnvelopeWs envelope)
    {
        DroneTelemetryWs? telemetry;
        try
        {
            telemetry = envelope.Message.Deserialize<DroneTelemetryWs>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid telemetry payload");
            return;
        }
        if (telemetry == null) return;
        _manager.ProcessTelemetry(telemetry);
    }
}
