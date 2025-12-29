using dTITAN.Backend.Data.Persistence;

namespace dTITAN.Backend.Data.Models;

public sealed class Drone
{
    public string DroneId { get; init; } = default!;
    public string Model { get; set; } = default!;

    public DateTime FirstSeenAt { get; init; }
    public bool IsConnected { get; set; }

    public Telemetry Telemetry { get; set; } = default!;

    public static Drone From(DroneSnapshotDocument doc) => new()
    {
        DroneId = doc.DroneId,
        Model = doc.Model,
        FirstSeenAt = doc.FirstSeenAt,
        IsConnected = doc.IsConnected,
        Telemetry = doc.Telemetry
    };
}