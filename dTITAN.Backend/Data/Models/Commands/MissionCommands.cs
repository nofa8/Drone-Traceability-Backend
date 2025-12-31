using dTITAN.Backend.Data.Models.Commands.Enums;
using dTITAN.Backend.Data.Transport.Websockets;

namespace dTITAN.Backend.Data.Models.Commands;

/// <summary>
/// Command to start a mission profile containing waypoints and actions.
/// </summary>
public class StartMissionCommand : DroneCommand, IHasAllowedCommands
{
    public StartMissionCommand() { Command = "startMission"; }
    public static IReadOnlyList<string> AllowedCommands => ["startMission"];

    /// <summary>
    /// Action to perform before the mission starts.
    /// </summary>
    public StartAction StartAction { get; set; }

    /// <summary>
    /// Action to perform after completing the mission.
    /// </summary>
    public EndAction EndAction { get; set; }

    /// <summary>
    /// Number of times to repeat the mission (0 = once).
    /// </summary>
    public int? Repeat { get; set; }

    /// <summary>
    /// Mission flight altitude in meters.
    /// </summary>
    public double Altitude { get; set; }

    /// <summary>
    /// Ordered list of waypoints comprising the mission path.
    /// </summary>
    public List<GeoPointWs> Path { get; set; } = [];

    /// <summary>
    /// Initial mission status.
    /// </summary>
    public MissionStatus Status { get; set; }
}

