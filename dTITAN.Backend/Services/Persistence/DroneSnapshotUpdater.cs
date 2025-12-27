using MongoDB.Driver;
using dTITAN.Backend.Data.Documents;
using dTITAN.Backend.EventBus;
using dTITAN.Backend.Data.Events;

namespace dTITAN.Backend.Services.Persistence;

public class DroneSnapshotWriter
{
    private readonly IMongoCollection<DroneSnapshotDocument> _collection;

    public DroneSnapshotWriter(IMongoCollection<DroneSnapshotDocument> collection, IDroneEventBus eventBus)
    {
        _collection = collection;
        eventBus.Subscribe<DroneTelemetryReceived>(HandleTelemetryReceived);
    }

    private async Task HandleTelemetryReceived(DroneTelemetryReceived evt)
    {
        DroneSnapshotDocument doc = new DroneSnapshotDocument().FromEvent(evt);
        await _collection.ReplaceOneAsync(
            s => s.DroneId == doc.DroneId,
            doc,
            new ReplaceOptions { IsUpsert = true }
        );
    }
}
