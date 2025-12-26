namespace dTITAN.Backend.Data.Transport.Websockets;

public sealed class VirtualSticksInputCommand : DroneCommand
{
    public double Yaw { get; set; }
    public double Pitch { get; set; }
    public double Roll { get; set; }
    public double Throttle { get; set; }
}
