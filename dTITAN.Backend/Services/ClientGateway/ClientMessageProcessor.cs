using System.Text.Json;
using System.Threading.Channels;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Models.Events;
using dTITAN.Backend.Data.Models.Commands;
using dTITAN.Backend.Data.Transport.Websockets;
using dTITAN.Backend.Services.EventBus;

namespace dTITAN.Backend.Services.ClientGateway;

public sealed class ClientMessageProcessor(
    Channel<(Guid id, string message)> channel,
    IEventBus eventBus,
    ILogger<ClientMessageProcessor> logger) : BackgroundService
{
    private readonly Channel<(Guid id, string message)> _channel = channel;
    private readonly ILogger<ClientMessageProcessor> _logger = logger;
    private readonly IEventBus _eventBus = eventBus;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var (id, message)
            in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            var now = DateTime.UtcNow;
            ExternalEnvelope? envelope = null;
            _logger.LogInformation("Received message from {ClientId}: {Message}", id, message);
            try
            {
                envelope = JsonSerializer.Deserialize<ExternalEnvelope>(message, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize incoming message as EventEnvelope from {ClientId}", id);
                continue;
            }

            if (envelope == null)
            {
                _logger.LogWarning("Empty envelope received from {ClientId}", id);
                continue;
            }

            try
            {
                DroneCommand command = envelope.Role switch
                {
                    nameof(FlightCommand) => ParseCommand<FlightCommand>(envelope.Message),
                    nameof(UtilityCommand) => ParseCommand<UtilityCommand>(envelope.Message),
                    nameof(StartMissionCommand) => ParseCommand<StartMissionCommand>(envelope.Message),
                    nameof(VirtualSticksInputCommand) => ParseCommand<VirtualSticksInputCommand>(envelope.Message),
                    _ => throw new InvalidOperationException($"Unknown command role '{envelope.Role}'")
                };

                _logger.LogInformation("Received command '{Command}' from {ClientId}", command, id);
                var evt = new CommandReceived(new DroneCommandContext(id, envelope.UserId, command), now);
                _eventBus.Publish(evt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling envelope of type {Role} from {ClientId}", envelope.Role, id);
            }
        }
    }

    private static DroneCommand ParseCommand<T>(JsonElement message) where T : DroneCommand, IHasAllowedCommands
    {
        var cmd = CheckBaseCommand(message);
        var allowed = T.AllowedCommands;
        if (allowed.Contains(cmd) == false)
        {
            throw new InvalidOperationException($"Unknown command {typeof(T).Name}: '{cmd}'");
        }
        var result = JsonSerializer.Deserialize<T>(message, _jsonOptions);
        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize command");
        }
        return result;
    }

    private static string CheckBaseCommand(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Unexpected payload shape");
        }
        if (!payload.TryGetProperty("command", out var cmdProp))
        {
            throw new InvalidOperationException("Missing command property");
        }
        var cmd = cmdProp.GetString();
        if (string.IsNullOrEmpty(cmd))
        {
            throw new InvalidOperationException("Missing command property");
        }
        return cmd;
    }
}
