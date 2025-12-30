namespace dTITAN.Backend.Data.Models;

public sealed class CommandStatus
{
    public Guid ConnectionId { get; set; }
    public string DroneId { get; set; } = default!;
    public CommandState CommandState { get; set; } = default!;
}