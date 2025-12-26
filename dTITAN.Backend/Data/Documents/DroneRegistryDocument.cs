using dTITAN.Backend.Data.Transport.Websockets;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dTITAN.Backend.Data.Documents;

public class DroneRegistryDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string? Id { get; set; }
    public string DroneId { get; set; } = default!;
    public string Model { get; set; } = default!;
    public bool IsConnected { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }

    public bool Equals(DroneRegistryDocument other)
    {
        return other != null &&
               DroneId == other.DroneId &&
               Model == other.Model;
    }
}
