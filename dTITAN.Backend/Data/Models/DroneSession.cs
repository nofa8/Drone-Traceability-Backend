namespace dTITAN.Backend.Data.Models;

public sealed class DroneSession(string droneId, DateTime lastSeen)
{
    public string DroneId { get; } = droneId;
    public DateTime LastSeen { get; set; } = lastSeen;
}