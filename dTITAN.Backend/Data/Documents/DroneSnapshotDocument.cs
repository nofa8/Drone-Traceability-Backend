using dTITAN.Backend.Data.Events;

namespace dTITAN.Backend.Data.Documents;


/// <summary>
/// Represents a snapshot of the latest telemetry of a drone,
/// inheriting from DroneTelemetryDocument.
/// </summary>
public class DroneSnapshotDocument : DroneTelemetryDocument
{
    // Avoids casting issues by returning the correct type
    public new DroneSnapshotDocument FromEvent(DroneTelemetryReceived evt)
    {
        base.FromEvent(evt);
        return this;
    }   
}