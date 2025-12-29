using dTITAN.Backend.Data.Persistence;

namespace dTITAN.Backend.Data.Models;

public sealed class DroneSnapshot: DroneTelemetry
{
    public DateTime FirstSeenAt { get; init; }
    public bool IsConnected { get; set; }

    public static DroneSnapshot From(DroneSnapshotDocument doc) => new()
    {
        DroneId = doc.DroneId,
        Model = doc.Model,
        FirstSeenAt = doc.FirstSeenAt,
        IsConnected = doc.IsConnected,
        Telemetry = doc.Telemetry
    };
}