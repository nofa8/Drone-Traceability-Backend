using Microsoft.Extensions.Logging;

namespace dTITAN.Backend.Services;

/// <summary>
/// Background service that initializes the <see cref="DroneConnectionManager"/>
/// and starts connections for all configured drones. The service reads drone
/// identifiers and launches the manager which runs per-drone connection tasks.
/// </summary>
public class WebSocketService : BackgroundService
{
    private readonly DroneConnectionManager _manager;
    private readonly ILogger<WebSocketService> _logger;

    /// <summary>
    /// Constructs the <see cref="WebSocketService"/>, validating configuration
    /// and preparing the <see cref="DroneConnectionManager"/> instance.
    /// </summary>
    /// <param name="config">Application configuration used to locate the WebSocket URI.</param>
    /// <param name="queue">Shared message queue for incoming drone messages.</param>
    /// <param name="loggerFactory">Factory to create per-connection loggers.</param>
    /// <param name="logger">Logger for this service.</param>
    /// <exception cref="InvalidOperationException">Thrown when the DroneWS connection string is not configured.</exception>
    public WebSocketService(
        IConfiguration config,
        DroneMessageQueue queue,
        ILoggerFactory loggerFactory,
        ILogger<WebSocketService> logger)
    {
        _logger = logger;

        var connectionString = config.GetConnectionString("DroneWS");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Drone WebSocket not configured.");

        _logger.LogInformation("Initializing WebSocketService with {Uri}", connectionString);

        _manager = new DroneConnectionManager(
            new Uri(connectionString),
            queue,
            loggerFactory
        );
    }

    /// <summary>
    /// Entry point for the background service. Loads drone identifiers and
    /// starts the connection manager which runs until cancellation.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token signaling shutdown.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var droneIds = await LoadDroneIdsAsync(stoppingToken);
        _logger.LogInformation("Starting drone connection manager for {Count} drones", droneIds.Count());
        await _manager.StartAsync(droneIds, stoppingToken);
    }

    /// <summary>
    /// Loads the set of drone identifiers to connect to. This is a placeholder
    /// implementation and should be replaced with a database or configuration
    /// source in production.
    /// </summary>
    /// <param name="ct">Cancellation token for the load operation.</param>
    /// <returns>Enumerable of drone ids.</returns>
    private Task<IEnumerable<string>> LoadDroneIdsAsync(
        CancellationToken ct)
    {
        // TODO: Replace with DB / API / config
        IEnumerable<string> ids = new[] { "1", "2", "3" };

        return Task.FromResult(ids);
    }
}

