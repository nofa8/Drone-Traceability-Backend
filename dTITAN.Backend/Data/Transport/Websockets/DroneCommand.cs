namespace dTITAN.Backend.Data.Transport.Websockets;

public abstract class DroneCommand
{
    public string Command { get; set; } = default!;
}
