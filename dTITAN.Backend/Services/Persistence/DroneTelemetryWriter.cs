using MongoDB.Driver;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Services.EventBus;
using dTITAN.Backend.Data.Persistence;

namespace dTITAN.Backend.Services.Persistence;

public class DroneTelemetryWriter
{
    private readonly IMongoCollection<DroneTelemetryDocument> _telemetries;

    public DroneTelemetryWriter(IMongoCollection<DroneTelemetryDocument> telemetries, IDroneEventBus eventBus)
    {
        _telemetries = telemetries;
        eventBus.Subscribe<DroneTelemetryReceived>(HandleTelemetryReceived);
    }

    private async Task HandleTelemetryReceived(DroneTelemetryReceived evt)
    {
        var doc = DroneTelemetryDocument.From(evt.DroneTelemetry);
        await _telemetries.InsertOneAsync(doc);
    }
}
