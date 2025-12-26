namespace dTITAN.Backend.Data.Transport.Websockets;

public sealed class StartMissionCommand : DroneCommand
{
    public string StartAction { get; set; } = default!;
    public string EndAction { get; set; } = default!;
    public int Repeat { get; set; }
    public double Altitude { get; set; }
    public List<GeoPoint> Path { get; set; } = [];
    public string Status { get; set; } = default!;
}
