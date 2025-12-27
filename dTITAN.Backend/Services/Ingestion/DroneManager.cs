using System.Collections.Concurrent;
using dTITAN.Backend.Data.Events;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.EventBus;

namespace dTITAN.Backend.Services.Ingestion;

public sealed class DroneManager(IDroneEventBus eventBus, TimeSpan timeout)
{
    private readonly IDroneEventBus _eventBus = eventBus;
    private readonly TimeSpan _timeout = timeout;
    private readonly ConcurrentDictionary<string, DroneSession> _sessions = new();
    // priority queue: earliest expiration first
    private readonly PriorityQueue<string, DateTime> _expirationQueue = new();
    private readonly Lock _expirationLock = new();


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

        // calculate expiration and enqueue
        var expiration = now + _timeout;
        lock (_expirationLock)
        {
            _expirationQueue.Enqueue(id, expiration);
        }

        _eventBus.Publish(new DroneTelemetryReceived(telemetry, now));
    }

    public void SweepDisconnected()
    {
        var now = DateTime.UtcNow;

        while (true)
        {
            string? idToExpire;
            DateTime expirationTime;

            lock (_expirationLock)
            {
                if (_expirationQueue.Count == 0) break;
                _expirationQueue.TryPeek(out idToExpire, out expirationTime);
                if (expirationTime > now) break;
                _expirationQueue.TryDequeue(out _, out _);
            }

            if (idToExpire != null &&
                _sessions.TryGetValue(idToExpire, out var session) &&
                session.LastSeen + _timeout <= now)
            {
                if (_sessions.TryRemove(idToExpire, out _))
                {
                    _eventBus.Publish(new DroneDisconnected(idToExpire));
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
