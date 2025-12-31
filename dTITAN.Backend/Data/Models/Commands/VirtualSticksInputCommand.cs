namespace dTITAN.Backend.Data.Models.Commands;

public class VirtualSticksInputCommand : DroneCommand, IHasAllowedCommands
{
    public VirtualSticksInputCommand() { Command = "virtualSticksInput"; }
    public static IReadOnlyList<string> AllowedCommands => ["virtualSticksInput"];

    /// <summary>
    /// Rotation around vertical axis.
    /// Range: -1.0 (left) to 1.0 (right).
    /// </summary>
    public double Yaw { get; set; }

    /// <summary>
    /// Forward/backward movement.
    /// Range: -1.0 (forward) to 1.0 (backward).
    /// </summary>
    public double Pitch { get; set; }

    /// <summary>
    /// Left/right movement.
    /// Range: -1.0 (left) to 1.0 (right).
    /// </summary>
    public double Roll { get; set; }

    /// <summary>
    /// Vertical movement.
    /// Range: -1.0 (down) to 1.0 (up).
    /// </summary>
    public double Throttle { get; set; }
}