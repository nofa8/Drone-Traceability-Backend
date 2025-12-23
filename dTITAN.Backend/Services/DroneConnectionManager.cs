using dTITAN.Backend.EventBus;

namespace dTITAN.Backend.Services;

public sealed class DroneConnectionManager(
    Uri baseUri,
    IDroneEventBus eventBus,
    ILoggerFactory loggerFactory)
{
    private readonly Uri _baseUri = baseUri;
    private readonly IDroneEventBus _eventBus = eventBus;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public Task StartAsync(
        IEnumerable<string> droneIds,
        CancellationToken ct)
    {
        var logger = _loggerFactory.CreateLogger<DroneConnectionManager>();
        var ids = droneIds.ToList();
        logger.LogInformation("Starting connections for {Count} drones", ids.Count);

        var tasks = ids.Select(droneId =>
        {
            logger.LogDebug("Launching connection task for drone {DroneId}", droneId);
            var conn = new DroneConnection(droneId, _baseUri, _eventBus, _loggerFactory.CreateLogger<DroneConnection>());
            return Task.Run(() => conn.RunAsync(ct), ct);
        });

        return Task.WhenAll(tasks);
    }
}
