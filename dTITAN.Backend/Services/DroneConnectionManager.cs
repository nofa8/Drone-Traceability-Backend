namespace dTITAN.Backend.Services;

/// <summary>
/// Supervises connection instances for multiple drones and starts their
/// background tasks. This component is responsible for creating per-drone
/// <see cref="DroneConnection"/> instances and running them concurrently.
/// </summary>
/// <param name="baseUri">Base WebSocket URI for connecting to drones.</param>
/// <param name="queue">Shared message queue used by connections to publish messages.</param>
/// <param name="loggerFactory">Factory used to create loggers for individual connections.</param>
public sealed class DroneConnectionManager(
    Uri baseUri,
    DroneMessageQueue queue,
    ILoggerFactory loggerFactory)
{
    private readonly Uri _baseUri = baseUri;
    private readonly DroneMessageQueue _queue = queue;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public Task StartAsync(
        IEnumerable<string> droneIds,
        CancellationToken ct)
    {
        var tasks = droneIds.Select(droneId =>
        {
            var conn = new DroneConnection(droneId, _baseUri, _queue, _loggerFactory.CreateLogger<DroneConnection>());
            return Task.Run(() => conn.RunAsync(ct), ct);
        });

        return Task.WhenAll(tasks);
    }
}
