using dTITAN.Backend.Data.Models;

namespace dTITAN.Backend.Data.Events;

public interface IDroneEvent { }

public record DroneTelemetryReceived(DroneTelemetry Drone, DateTime ReceivedAt) : IDroneEvent;
public record DroneConnected(DroneTelemetry Drone, DateTime ReceivedAt) : IDroneEvent;
public record DroneDisconnected(string DroneId) : IDroneEvent;
