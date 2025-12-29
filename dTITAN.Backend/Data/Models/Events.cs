namespace dTITAN.Backend.Data.Models;

public interface IDroneEvent { }
public record DroneTelemetryReceived(DroneTelemetry DroneTelemetry, DateTime TimeStamp) : IDroneEvent;
public record DroneDisconnected(string DroneId, DateTime TimeStamp) : IDroneEvent;
