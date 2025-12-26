namespace dTITAN.Backend.Data.Transport.Websockets;

public sealed class CommandEnvelope
{
    public string UserId { get; set; } = default!;
    public DroneCommand Message { get; set; } = default!;
}
