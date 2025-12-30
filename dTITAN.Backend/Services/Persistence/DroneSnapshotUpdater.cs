using MongoDB.Driver;
using dTITAN.Backend.Services.EventBus;
using dTITAN.Backend.Data.Persistence;
using dTITAN.Backend.Data.Models.Events;
using MongoDB.Bson;

namespace dTITAN.Backend.Services.Persistence;

public class DroneSnapshotUpdater
{
    private readonly IMongoCollection<DroneSnapshotDocument> _snapshots;
    private readonly ILogger<DroneSnapshotUpdater> _logger;

    public DroneSnapshotUpdater(IMongoCollection<DroneSnapshotDocument> snapshots, IEventBus eventBus, ILogger<DroneSnapshotUpdater> logger)
    {
        _snapshots = snapshots;
        _logger = logger;
        eventBus.Subscribe<DroneTelemetryReceived>(HandleTelemetryReceived);
        eventBus.Subscribe<DroneDisconnected>(HandleDroneDisconnected);
    }

    private async Task HandleTelemetryReceived(DroneTelemetryReceived evt)
    {
        var d = evt.DroneTelemetry;
        var telemetry = d.Telemetry;
        var telemetryBson = telemetry.ToBsonDocument();

        var f = Builders<DroneSnapshotDocument>.Filter;
        var filter = f.And(
            f.Eq(x => x.DroneId, d.DroneId),
            f.Lt(x => x.Telemetry.Timestamp, telemetry.Timestamp)
        );

        var update = Builders<DroneSnapshotDocument>.Update
            .Set(x => x.Model, d.Model)
            .Set(x => x.IsConnected, true)
            .Set(x => x.Telemetry, telemetry);

        var result = await _snapshots.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<DroneSnapshotDocument> { IsUpsert = false }
        );

        if (result != null) return;

        try
        {
            await _snapshots.InsertOneAsync(new DroneSnapshotDocument
            {
                DroneId = d.DroneId,
                Model = d.Model,
                Telemetry = telemetry,
                IsConnected = true,
                FirstSeenAt = telemetry.Timestamp
            });
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Another writer inserted first, safe to ignore
        }
    }

    private async Task HandleDroneDisconnected(DroneDisconnected evt)
    {
        try
        {
            var filter = Builders<DroneSnapshotDocument>.Filter.Eq(s => s.DroneId, evt.DroneId);
            var update = Builders<DroneSnapshotDocument>.Update
                .Set(d => d.IsConnected, false);

            await _snapshots.UpdateOneAsync(filter, update);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to update drone snapshot for DroneId {DroneId} on disconnect", evt.DroneId);
        }
    }
}
