using dTITAN.Backend.Data.Models.Commands;

namespace dTITAN.Backend.Data.Persistence;

public sealed class DroneCommandDocument
{
    public Guid ConnectionId { get; set; }
    public string DroneId { get; set; } = default!;
    public string CommandType { get; set; } = default!;
    public DroneCommand Command { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? FailureReason { get; set; }
}