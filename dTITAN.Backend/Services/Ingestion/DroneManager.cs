using System.Collections.Concurrent;
using dTITAN.Backend.Data.Events;
using dTITAN.Backend.Data.Transport.Websockets;
using dTITAN.Backend.EventBus;

namespace dTITAN.Backend.Services.Ingestion;

public sealed class DroneManager(IDroneEventBus eventBus, TimeSpan? timeout = null)
{
    private readonly ConcurrentDictionary<string, DroneSession> _droneSessions = new();
    private readonly IDroneEventBus _eventBus = eventBus;
    private readonly TimeSpan _timeout = timeout ?? TimeSpan.FromSeconds(10);

    public void ProcessTelemetry(DroneTelemetry telemetry)
    {
        var now = DateTime.UtcNow;
        var droneId = telemetry.Id;

        var session = _droneSessions.GetOrAdd(droneId, id =>
        {
            _eventBus.Publish(new DroneConnected(telemetry, now));
            return new DroneSession(id, now);
        });

        session.LastSeen = now;
        _eventBus.Publish(new DroneTelemetryReceived(telemetry, now));
    }

    public void SweepDisconnected()
    {
        var now = DateTime.UtcNow;

        foreach (var (id, session) in _droneSessions)
        {
            if (now - session.LastSeen > _timeout)
            {
                if (_droneSessions.TryRemove(id, out _))
                {
                    _eventBus.Publish(new DroneDisconnected(id));
                }
            }
        }
    }
}

public sealed class DroneSession(string droneId, DateTime lastSeen)
{
    public string DroneId { get; } = droneId;
    public DateTime LastSeen { get; set; } = lastSeen;
}
