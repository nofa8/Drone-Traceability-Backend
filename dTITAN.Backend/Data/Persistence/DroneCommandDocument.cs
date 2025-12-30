using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Models.Commands;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dTITAN.Backend.Data.Persistence;

public sealed class DroneCommandDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string? Id { get; set; } = default!;
    public DateTime TimeStamp { get; set; }
    public string DroneId { get; set; } = default!;
    [BsonRepresentation(BsonType.String)]
    public Guid ConnectionId { get; set; }
    public string CommandType { get; set; } = default!;
    public DroneCommand Command { get; set; } = default!;

    public static DroneCommandDocument From(DroneCommandContext command, DateTime timeStamp) => new()
    {
        TimeStamp = timeStamp,
        DroneId = command.DroneId,
        ConnectionId = command.ConnectionId,
        CommandType = command.Command.GetType().Name,
        Command = command.Command,
    };
}