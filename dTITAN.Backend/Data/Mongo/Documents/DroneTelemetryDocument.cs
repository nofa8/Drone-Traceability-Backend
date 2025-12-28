using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dTITAN.Backend.Data.Mongo.Documents;

public class DroneTelemetryDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string? Id { get; set; }
    public string DroneId { get; set; } = default!;
    public DateTime Timestamp { get; set; }
    public GeoPointDocument HomeLocation { get; set; } = new GeoPointDocument();
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
    public double VelocityZ { get; set; }
    public double BatteryLevel { get; set; }
    public double BatteryTemperature { get; set; }
    public double Heading { get; set; }
    public int SatelliteCount { get; set; }
    public int RemainingFlightTime { get; set; }
    public bool IsTraveling { get; set; }
    public bool IsFlying { get; set; }
    public bool Online { get; set; }
    public bool IsGoingHome { get; set; }
    public bool IsHomeLocationSet { get; set; }
    public bool AreMotorsOn { get; set; }
    public bool AreLightsOn { get; set; }
}
