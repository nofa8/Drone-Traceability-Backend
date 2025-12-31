using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using dTITAN.Backend.Data.Models;

namespace dTITAN.Backend.Data.Persistence;

public sealed class DroneSnapshotDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string? Id { get; set; } = default!;
    public string DroneId { get; set; } = default!;

    public string Model { get; set; } = default!;
    public bool IsConnected { get; set; }
    public DateTime FirstSeenAt { get; set; }

    public Telemetry Telemetry { get; set; } = default!;
}
