using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using dTITAN.Backend.Data.Models;

namespace dTITAN.Backend.Data.Persistence;

public sealed class DroneCommandDocument : DroneCommandContext
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string? Id { get; set; } = default!;

    public static DroneCommandDocument From(DroneCommandContext command, DateTime timeStamp) => new()
    {
        TimeStamp = timeStamp,
        ConnectionId = command.ConnectionId,
        DroneId = command.DroneId,
        CommandType = command.CommandType,
        Command = command.Command
    };
}