using MongoDB.Driver;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Mongo.Documents;
using dTITAN.Backend.Services.EventBus;

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
        var d = evt.Drone;
        var ts = evt.ReceivedAt;

        var filter = Builders<DroneSnapshotDocument>.Filter
            .Eq(s => s.DroneId, d.Id);

        var update = Builders<DroneSnapshotDocument>.Update
            .Set(s => s.Model, d.Model)
            .Set(s => s.IsConnected, true)
            .SetOnInsert(s => s.FirstSeenAt, ts)
            .Set(s => s.Timestamp, ts)
            .Set(s => s.IsHomeLocationSet, d.IsHomeLocationSet)
            .Set(s => s.Latitude, d.Latitude)
            .Set(s => s.Longitude, d.Longitude)
            .Set(s => s.Altitude, d.Altitude)
            .Set(s => s.VelocityX, d.VelocityX)
            .Set(s => s.VelocityY, d.VelocityY)
            .Set(s => s.VelocityZ, d.VelocityZ)
            .Set(s => s.BatteryLevel, d.BatteryLevel)
            .Set(s => s.BatteryTemperature, d.BatteryTemperature)
            .Set(s => s.Heading, d.Heading)
            .Set(s => s.SatelliteCount, d.SatelliteCount)
            .Set(s => s.RemainingFlightTime, d.RemainingFlightTime)
            .Set(s => s.IsTraveling, d.IsTraveling)
            .Set(s => s.IsFlying, d.IsFlying)
            .Set(s => s.Online, d.Online)
            .Set(s => s.IsGoingHome, d.IsGoingHome)
            .Set(s => s.AreMotorsOn, d.AreMotorsOn)
            .Set(s => s.AreLightsOn, d.AreLightsOn);

        await _snapshots.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = true }
        );

    }
    private async Task HandleDroneDisconnected(DroneDisconnected evt)
    {
        var filter = Builders<DroneSnapshotDocument>.Filter.Eq(d => d.DroneId, evt.DroneId);
        var update = Builders<DroneSnapshotDocument>.Update
            .Set(d => d.IsConnected, false);

        await _snapshots.UpdateOneAsync(filter, update);
    }
}
