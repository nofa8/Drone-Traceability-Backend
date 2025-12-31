using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using dTITAN.Backend.Data.Models.Commands;

namespace dTITAN.Backend.Data.Models;

public class DroneCommandContext
{
    public DateTime TimeStamp { get; set; }
    [BsonRepresentation(BsonType.String)]
    public Guid ConnectionId { get; set; } = default!;
    public string DroneId { get; set; } = default!;
    public string CommandType { get; set; } = default!;
    public DroneCommand Command { get; set; } = default!;
    public static DroneCommandContext From(Guid connectionId, string droneId, DroneCommand command, DateTime timestamp) => new()
    {
        TimeStamp = timestamp,
        ConnectionId = connectionId,
        DroneId = droneId,
        CommandType = command.GetType().Name,
        Command = command
    };
}
