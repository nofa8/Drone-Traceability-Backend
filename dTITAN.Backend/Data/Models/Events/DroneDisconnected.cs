namespace dTITAN.Backend.Data.Models.Events;

public record DroneDisconnected(string DroneId, DateTime TimeStamp) : IDroneEvent
{
    public string EventType => nameof(DroneDisconnected);
    public object ToPayload() => DroneId;
}
