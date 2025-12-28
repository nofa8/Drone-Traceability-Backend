namespace dTITAN.Backend.Data.Mongo.Documents;


/// <summary>
/// Represents a snapshot of the latest telemetry of a drone and metadata of the drone,
/// inheriting from DroneTelemetryDocument.
/// </summary>
public class DroneSnapshotDocument : DroneTelemetryDocument
{
    public string Model { get; set; } = default!;
    public bool IsConnected { get; set; }
    public DateTime FirstSeenAt { get; set; }
}