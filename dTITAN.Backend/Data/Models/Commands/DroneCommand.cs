using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace dTITAN.Backend.Data.Models.Commands;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "_t")]
[JsonDerivedType(typeof(FlightCommand))]
[JsonDerivedType(typeof(StartMissionCommand))]
[JsonDerivedType(typeof(UtilityCommand))]
[JsonDerivedType(typeof(VirtualSticksInputCommand))]
[BsonKnownTypes(
    typeof(FlightCommand),
    typeof(StartMissionCommand),
    typeof(UtilityCommand),
    typeof(VirtualSticksInputCommand)
)]
public abstract class DroneCommand
{
    public string Command { get; set; } = default!;
}