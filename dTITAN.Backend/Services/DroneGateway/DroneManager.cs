using System.Collections.Concurrent;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Models.Events;
using dTITAN.Backend.Data.Transport.Websockets;
using dTITAN.Backend.Services.EventBus;

namespace dTITAN.Backend.Services.DroneGateway;

public sealed class DroneManager(IEventBus eventBus, TimeSpan timeout, ILogger<DroneManager> logger)
{
    private readonly IEventBus _eventBus = eventBus;
    private readonly TimeSpan _timeout = timeout;
    private readonly ILogger<DroneManager> _logger = logger;
    private readonly ConcurrentDictionary<string, DroneSession> _sessions = new();

    public void ProcessTelemetry(DroneTelemetryWs telemetry)
    {
        var now = DateTime.UtcNow;
        var id = telemetry.Id;

        var session = _sessions.GetOrAdd(id, _ => new DroneSession(id, now));
        session.LastSeen = now;

        _logger.LogDebug(
            "Telemetry {Id}: [{Lat}, {Lng}, {Alt}]",
            id,
            telemetry.Latitude,
            telemetry.Longitude,
            telemetry.Altitude
        );

        var droneTelemetry = DroneTelemetry.From(telemetry, now);
        _eventBus.Publish(new DroneTelemetryReceived(droneTelemetry, now));
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
                    _eventBus.Publish(new DroneDisconnected(id, now));
                    disconnectedCount++;
                }
            }
        }
        _logger.LogDebug("Disconnected {Count} drones", disconnectedCount);
    }
}
