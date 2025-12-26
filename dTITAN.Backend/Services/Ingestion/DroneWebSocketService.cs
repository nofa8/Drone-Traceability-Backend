using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Websockets;
using dTITAN.Backend.Data.Websockets.Commands;

namespace dTITAN.Backend.Services.Ingestion;

public sealed class DroneWebSocketClient : BackgroundService
{
    private readonly ILogger _logger;
    private readonly DroneManager _manager;
    private readonly Uri _uri;

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
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(_uri, ct);

        var buffer = new byte[8192];
        var segment = new ArraySegment<byte>(buffer);

        while (!ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await ws.ReceiveAsync(segment, ct);
                if (result.MessageType == WebSocketMessageType.Close) return;
                ms.Write(segment.Array!, segment.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            var payload = Encoding.UTF8.GetString(ms.ToArray());

            try
            {
                HandleMessage(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process incoming WS message");
            }
        }
    }

    private void HandleMessage(string payload)
    {
        WsEnvelope envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<WsEnvelope>(payload)!;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid WS envelope");
            return;
        }

        switch (envelope.Role)
        {
            case "drone":
                HandleTelemetry(envelope);
                break;

            default:
                _logger.LogDebug("Unknown role {Role}", envelope.Role);
                break;
        }
    }
    private void HandleTelemetry(WsEnvelope envelope)
    {
        DroneTelemetry? telemetry;

        try
        {
            telemetry = envelope.Message.Deserialize<DroneTelemetry>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid telemetry payload");
            return;
        }

        if (telemetry == null) return;

        _manager.ProcessTelemetry(telemetry);

        _logger.LogDebug(
            "Telemetry {Id}: [{Lat}, {Lng}, {Alt}]",
            envelope.Id,
            telemetry.Latitude,
            telemetry.Longitude,
            telemetry.Altitude
        );
    }

    private void HandleDroneCommandUpdate(DroneCommand command, string? userId)
    {
        // Optional logging / handling for command updates
        _logger.LogInformation("Received command update {CommandType} from {UserId}", command.GetType().Name, userId);
    }
}
