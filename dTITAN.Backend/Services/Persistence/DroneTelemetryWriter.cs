using MongoDB.Driver;
using dTITAN.Backend.Services.EventBus;
using dTITAN.Backend.Data.Persistence;
using dTITAN.Backend.Data.Models.Events;

namespace dTITAN.Backend.Services.Persistence;

public class DroneTelemetryWriter
{
    private readonly IMongoCollection<DroneTelemetryDocument> _telemetries;
    private readonly ILogger<DroneTelemetryWriter> _logger;

    public DroneTelemetryWriter(IMongoCollection<DroneTelemetryDocument> telemetries, IEventBus eventBus, ILogger<DroneTelemetryWriter> logger)
    {
        _telemetries = telemetries;
        _logger = logger;
        eventBus.Subscribe<DroneTelemetryReceived>(HandleTelemetryReceived);
    }

    private async Task HandleTelemetryReceived(DroneTelemetryReceived evt)
    {
        try
        {
            var doc = DroneTelemetryDocument.From(evt.DroneTelemetry);
            await _telemetries.InsertOneAsync(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write drone telemetry for DroneId {DroneId}", evt.DroneTelemetry.DroneId);
        }
    }
}
