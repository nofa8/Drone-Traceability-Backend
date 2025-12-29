namespace dTITAN.Backend.Data.Models.Events;

public record DroneTelemetryReceived(DroneTelemetry DroneTelemetry, DateTime TimeStamp) : IDroneEvent
{
    public string EventType => nameof(DroneTelemetryReceived);
}
