using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using dTITAN.Backend.Data.Models;

namespace dTITAN.Backend.Data.Persistence;

public class DroneTelemetryDocument : Telemetry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string? Id { get; set; } = default!;
    public string DroneId { get; set; } = default!;

    public static DroneTelemetryDocument From(DroneTelemetry dt) => new()
    {
        DroneId = dt.DroneId,

        Timestamp = dt.Telemetry.Timestamp,
        HomeLocation = dt.Telemetry.HomeLocation,

        Latitude = dt.Telemetry.Latitude,
        Longitude = dt.Telemetry.Longitude,
        Altitude = dt.Telemetry.Altitude,

        VelocityX = dt.Telemetry.VelocityX,
        VelocityY = dt.Telemetry.VelocityY,
        VelocityZ = dt.Telemetry.VelocityZ,

        BatteryLevel = dt.Telemetry.BatteryLevel,
        BatteryTemperature = dt.Telemetry.BatteryTemperature,

        Heading = dt.Telemetry.Heading,
        SatelliteCount = dt.Telemetry.SatelliteCount,
        RemainingFlightTime = dt.Telemetry.RemainingFlightTime,

        IsTraveling = dt.Telemetry.IsTraveling,
        IsFlying = dt.Telemetry.IsFlying,
        Online = dt.Telemetry.Online,
        IsGoingHome = dt.Telemetry.IsGoingHome,
        IsHomeLocationSet = dt.Telemetry.IsHomeLocationSet,
        AreMotorsOn = dt.Telemetry.AreMotorsOn,
        AreLightsOn = dt.Telemetry.AreLightsOn
    };
}