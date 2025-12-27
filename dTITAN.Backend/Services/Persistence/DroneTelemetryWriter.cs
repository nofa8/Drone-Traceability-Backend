using MongoDB.Driver;
using dTITAN.Backend.Data.Documents;
using dTITAN.Backend.EventBus;
using dTITAN.Backend.Data.Events;

namespace dTITAN.Backend.Services.Persistence;

public class DroneTelemetryWriter
{
    private readonly IMongoCollection<DroneTelemetryDocument> _collection;

    public DroneTelemetryWriter(IMongoCollection<DroneTelemetryDocument> collection, IDroneEventBus eventBus)
    {
        _collection = collection;
        eventBus.Subscribe<DroneTelemetryReceived>(HandleTelemetryReceived);
    }

    private async Task HandleTelemetryReceived(DroneTelemetryReceived evt)
    {
        var doc = new DroneTelemetryDocument().FromEvent(evt);
        await _collection.InsertOneAsync(doc);
    }
}
