using dTITAN.Backend.Data.Models.Commands;

namespace dTITAN.Backend.Data.Models;

public sealed class DroneCommandContext(Guid ConnectionId, string DroneId, DroneCommand Command)
{
    public Guid ConnectionId { get; set; } = ConnectionId;
    public string DroneId { get; set; } = DroneId;
    public DroneCommand Command { get; set; } = Command;
}