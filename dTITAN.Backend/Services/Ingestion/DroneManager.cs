using System.Collections.Concurrent;
using dTITAN.Backend.Data.Events;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.EventBus;

namespace dTITAN.Backend.Services.Ingestion;

public sealed class DroneManager(IDroneEventBus eventBus, TimeSpan timeout, ILogger<DroneManager> logger)
{
    private readonly IDroneEventBus _eventBus = eventBus;
    private readonly TimeSpan _timeout = timeout;
    private readonly ILogger<DroneManager> _logger = logger;
    private readonly ConcurrentDictionary<string, DroneSession> _sessions = new();

    public void ProcessTelemetry(DroneTelemetry telemetry)
    {
        var now = DateTime.UtcNow;
        var id = telemetry.Id;

        var session = _sessions.GetOrAdd(id, _ =>
        {
            _eventBus.Publish(new DroneConnected(telemetry, now));
            return new DroneSession(id, now);
        });

        session.LastSeen = now;
        _logger.LogDebug(
            "Telemetry {Id}: [{Lat}, {Lng}, {Alt}]",
            telemetry.Id,
            telemetry.Latitude,
            telemetry.Longitude,
            telemetry.Altitude
        );
        _eventBus.Publish(new DroneTelemetryReceived(telemetry, now));
    }

    public void SweepDisconnected()
    {
        if (_sessions.IsEmpty) return;
        var now = DateTime.UtcNow;
        _logger.LogDebug("Sweeping for disconnected drones at {Now}", now);
        var disconnectedCount = 0;
        foreach (var (id, session) in _sessions)
        {
            if (session.LastSeen + _timeout <= now)
            {
                if (_sessions.TryRemove(id, out _))
                {
                    _eventBus.Publish(new DroneDisconnected(id));
                    disconnectedCount++;
                }
            }
        }
        _logger.LogDebug("Disconnected {Count} drones", disconnectedCount);
    }

}

public sealed class DroneSession(string droneId, DateTime lastSeen)
{
    public string DroneId { get; } = droneId;
    public DateTime LastSeen { get; set; } = lastSeen;
}
