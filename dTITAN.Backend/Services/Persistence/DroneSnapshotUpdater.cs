using MongoDB.Driver;
using dTITAN.Backend.Data;
using dTITAN.Backend.Data.Documents;
using dTITAN.Backend.EventBus;
using dTITAN.Backend.Data.Events;
using dTITAN.Backend.Data.Models;

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
        var doc = new DroneSnapshotDocument
        {
            DroneId = evt.Drone.Id,
            Timestamp = evt.ReceivedAt,
            HomeLocation = new GeoPoint
            {
                Latitude = evt.Drone.HomeLocation.Latitude,
                Longitude = evt.Drone.HomeLocation.Longitude
            },
            Latitude = evt.Drone.Latitude,
            Longitude = evt.Drone.Longitude,
            Altitude = evt.Drone.Altitude,
            VelocityX = evt.Drone.VelocityX,
            VelocityY = evt.Drone.VelocityY,
            VelocityZ = evt.Drone.VelocityZ,
            BatteryLevel = evt.Drone.BatteryLevel,
            BatteryTemperature = evt.Drone.BatteryTemperature,
            Heading = evt.Drone.Heading,
            SatelliteCount = evt.Drone.SatelliteCount,
            RemainingFlightTime = evt.Drone.RemainingFlightTime,
            IsTraveling = evt.Drone.IsTraveling,
            IsFlying = evt.Drone.IsFlying,
            Online = evt.Drone.Online,
            IsGoingHome = evt.Drone.IsGoingHome,
            IsHomeLocationSet = evt.Drone.IsHomeLocationSet,
            AreMotorsOn = evt.Drone.AreMotorsOn,
            AreLightsOn = evt.Drone.AreLightsOn
        };

        await _collection.ReplaceOneAsync(
            s => s.DroneId == doc.DroneId,
            doc,
            new ReplaceOptions { IsUpsert = true }
        );
    }
}
