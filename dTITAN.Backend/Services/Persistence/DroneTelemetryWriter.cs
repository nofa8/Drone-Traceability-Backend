using MongoDB.Driver;
using dTITAN.Backend.Services.EventBus;
using dTITAN.Backend.Data.Persistence;
using dTITAN.Backend.Data.Models.Events;
using dTITAN.Backend.Data.Models;
using System.Collections.Concurrent;

namespace dTITAN.Backend.Services.Persistence;

public class DroneTelemetryWriter
{
    private readonly IMongoCollection<DroneTelemetryDocument> _telemetries;
    private readonly ILogger<DroneTelemetryWriter> _logger;

    private readonly ConcurrentDictionary<string, TelemetryPersistenceState> _states;

    public DroneTelemetryWriter(IMongoCollection<DroneTelemetryDocument> telemetries, IEventBus eventBus, ILogger<DroneTelemetryWriter> logger)
    {
        _telemetries = telemetries;
        _logger = logger;
        _states = new();

        eventBus.Subscribe<DroneTelemetryReceived>(HandleTelemetryReceived);
    }

    private async Task HandleTelemetryReceived(DroneTelemetryReceived evt)
    {
        var droneId = evt.DroneTelemetry.DroneId;
        var telemetry = evt.DroneTelemetry.Telemetry;
        var now = evt.TimeStamp;

        _states.TryGetValue(droneId, out var previousState);
        bool persist = TelemetryPersistencePolicy.ShouldPersist(telemetry, previousState, now);
        if (!persist) return;

        try
        {
            var doc = DroneTelemetryDocument.From(evt.DroneTelemetry);
            await _telemetries.InsertOneAsync(doc);

            var newState = new TelemetryPersistenceState
            {
                LastPersisted = telemetry,
                LastPersistedAt = now
            };

            _states.AddOrUpdate(
                droneId,
                newState,
                (_, __) => newState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write drone telemetry for DroneId {DroneId}", evt.DroneTelemetry.DroneId);
        }
    }
}
