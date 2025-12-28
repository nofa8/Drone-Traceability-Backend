using MongoDB.Driver;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Mongo.Documents;
using dTITAN.Backend.Services.EventBus;

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
        var d = evt.Drone;
        var doc = new DroneTelemetryDocument
        {
            DroneId = d.Id,
            Timestamp = evt.ReceivedAt,
            HomeLocation = new GeoPointDocument
            {
                Latitude = d.HomeLocation.Latitude,
                Longitude = d.HomeLocation.Longitude
            },
            Latitude = d.Latitude,
            Longitude = d.Longitude,
            Altitude = d.Altitude,
            VelocityX = d.VelocityX,
            VelocityY = d.VelocityY,
            VelocityZ = d.VelocityZ,
            BatteryLevel = d.BatteryLevel,
            BatteryTemperature = d.BatteryTemperature,
            Heading = d.Heading,
            SatelliteCount = d.SatelliteCount,
            RemainingFlightTime = d.RemainingFlightTime,
            IsTraveling = d.IsTraveling,
            IsFlying = d.IsFlying,
            Online = d.Online,
            IsGoingHome = d.IsGoingHome,
            IsHomeLocationSet = d.IsHomeLocationSet,
            AreMotorsOn = d.AreMotorsOn,
            AreLightsOn = d.AreLightsOn
        };
        await _telemetries.InsertOneAsync(doc);
    }
}
