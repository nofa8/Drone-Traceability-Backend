using MongoDB.Driver;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Services.EventBus;
using dTITAN.Backend.Data.Persistence;

namespace dTITAN.Backend.Services.Persistence;

public class DroneSnapshotUpdater
{
    private readonly IMongoCollection<DroneSnapshotDocument> _snapshots;

    public DroneSnapshotUpdater(IMongoCollection<DroneSnapshotDocument> snapshots, IDroneEventBus eventBus)
    {
        _snapshots = snapshots;
        eventBus.Subscribe<DroneTelemetryReceived>(HandleTelemetryReceived);
        eventBus.Subscribe<DroneDisconnected>(HandleDroneDisconnected);
    }

    private async Task HandleTelemetryReceived(DroneTelemetryReceived evt)
    {
        var d = evt.DroneTelemetry;
        var t = evt.DroneTelemetry.Telemetry;
        var ts = evt.TimeStamp;

        var filter = Builders<DroneSnapshotDocument>.Filter.And(
            Builders<DroneSnapshotDocument>.Filter.Eq(s => s.DroneId, d.DroneId),
            Builders<DroneSnapshotDocument>.Filter.Or(
                Builders<DroneSnapshotDocument>.Filter.Lt(s => s.Telemetry.Timestamp, ts),
                Builders<DroneSnapshotDocument>.Filter.Exists(s => s.Telemetry, false)
            )
        );


        var update = Builders<DroneSnapshotDocument>.Update
            .Set(s => s.Model, d.Model)
            .Set(s => s.IsConnected, true)
            .Set(s => s.Telemetry, d.Telemetry)
            .SetOnInsert(s => s.FirstSeenAt, ts);


        await _snapshots.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = true }
        );
    }
    private async Task HandleDroneDisconnected(DroneDisconnected evt)
    {
        var filter = Builders<DroneSnapshotDocument>.Filter.Eq(s => s.DroneId, evt.DroneId);
        var update = Builders<DroneSnapshotDocument>.Update
            .Set(d => d.IsConnected, false);

        await _snapshots.UpdateOneAsync(filter, update);
    }
}
