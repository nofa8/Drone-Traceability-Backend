using dTITAN.Backend.Models;

namespace dTITAN.Backend.Events;

public interface IDroneEvent { }

public record DroneTelemetryReceived(Drone Drone) : IDroneEvent;
public record DroneConnected(string DroneId) : IDroneEvent;
public record DroneDisconnected(string DroneId) : IDroneEvent;
