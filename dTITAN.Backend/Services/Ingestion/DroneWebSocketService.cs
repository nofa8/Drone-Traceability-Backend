using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using dTITAN.Backend.Data.Models.Events;
using dTITAN.Backend.Data.Transport.Websockets;
using dTITAN.Backend.Services.EventBus;

namespace dTITAN.Backend.Services.Ingestion;

public sealed class DroneWebSocketClient : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<DroneWebSocketClient> _logger;
    private readonly DroneManager _manager;
    private readonly Uri _uri;
    private ClientWebSocket? _currentSocket;
    private readonly object _socketLock = new();

    private readonly Channel<string> _inChannel = Channel.CreateUnbounded<string>();
    private readonly Channel<string> _outChannel = Channel.CreateUnbounded<string>();

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public DroneWebSocketClient(
        IConfiguration config,
        IEventBus eventBus,
        DroneManager manager,
        ILogger<DroneWebSocketClient> logger)
    {
        _eventBus = eventBus;
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
        _eventBus.Subscribe<IInternalEvent>(HandleCommandRequest);

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
                lock (_socketLock)
                {
                    _currentSocket = ws;
                }

                // start background
                Task sendTask = SendLoop(ws, ct);
                Task processTask = ProcessMessages(ct);

                await ReceiveLoop(ws, ct);

                // complete channel so processor finishes
                _inChannel.Writer.Complete();
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
            finally
            {
                lock (_socketLock)
                {
                    _currentSocket = null;
                }
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
            await _inChannel.Writer.WriteAsync(text, ct);
        }
    }
    private async Task SendLoop(ClientWebSocket ws, CancellationToken ct)
    {
        await foreach (var msg in _outChannel.Reader.ReadAllAsync(ct))
        {
            if (ws.State != WebSocketState.Open)
                continue;

            var bytes = Encoding.UTF8.GetBytes(msg);
            var segment = new ArraySegment<byte>(bytes);

            await ws.SendAsync(
                segment,
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: ct
            );
        }
    }


    private async Task ProcessMessages(CancellationToken ct)
    {
        await foreach (var payload in _inChannel.Reader.ReadAllAsync(ct))
        {
            ExternalEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<ExternalEnvelope>(payload, _jsonOptions);
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

    private void HandleTelemetry(ExternalEnvelope envelope)
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

    private Task HandleCommandRequest(IInternalEvent evt)
    {
        if (evt is not CommandReceived cmd) return Task.CompletedTask;

        var envelope = new ExternalEnvelope
        {
            Id = cmd.Command.DroneId,
            Message = JsonSerializer.SerializeToElement(cmd.ToPayload(), _jsonOptions)
        };
        var json = JsonSerializer.Serialize(envelope, _jsonOptions);
        _logger.LogInformation("Sending command to drone:\n{json}", json);
        return _outChannel.Writer.WriteAsync(json).AsTask();
    }

}
