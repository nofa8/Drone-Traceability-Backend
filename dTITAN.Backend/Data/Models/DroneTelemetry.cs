using dTITAN.Backend.Data.Transport.Websockets;

namespace dTITAN.Backend.Data.Models;

public class DroneTelemetry
{
    public string DroneId { get; init; } = default!;
    public string Model { get; set; } = default!;
    public Telemetry Telemetry { get; set; } = default!;

    public static DroneTelemetry From(DroneTelemetryWs ws, DateTime timestamp) => new()
    {
        DroneId = ws.Id,
        Model = ws.Model,
        Telemetry = Telemetry.From(ws, timestamp)
    };
}