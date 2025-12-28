namespace dTITAN.Backend.Data.Models;

public interface IDroneEvent { }
public record DroneTelemetryReceived(DroneTelemetry Drone, DateTime ReceivedAt) : IDroneEvent;
public record DroneDisconnected(string DroneId) : IDroneEvent;
