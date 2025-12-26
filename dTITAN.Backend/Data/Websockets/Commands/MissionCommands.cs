using System.Text.Json.Serialization;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Websockets.Commands.Enums;

namespace dTITAN.Backend.Data.Websockets.Commands;

/// <summary>
/// Command to start a mission profile containing waypoints and actions.
/// </summary>
public sealed class StartMissionCommand : DroneCommand
{
    public override string Command => "startMission";

    /// <summary>
    /// Action to perform before the mission starts.
    /// </summary>
    [JsonPropertyName("startAction")]
    public StartAction StartAction { get; set; }

    /// <summary>
    /// Action to perform after completing the mission.
    /// </summary>
    [JsonPropertyName("endAction")]
    public EndAction EndAction { get; set; }

    /// <summary>
    /// Number of times to repeat the mission (0 = once).
    /// </summary>
    [JsonPropertyName("repeat")]
    public int? Repeat { get; set; }

    /// <summary>
    /// Mission flight altitude in meters.
    /// </summary>
    [JsonPropertyName("altitude")]
    public double Altitude { get; set; }

    /// <summary>
    /// Ordered list of waypoints comprising the mission path.
    /// </summary>
    [JsonPropertyName("path")]
    public List<GeoPoint> Path { get; set; } = [];

    /// <summary>
    /// Initial mission status.
    /// </summary>
    [JsonPropertyName("status")]
    public MissionStatus Status { get; set; }
}

public sealed class PauseMissionCommand : DroneCommand
{
    public override string Command => "pauseMission";
}

public sealed class StopMissionCommand : DroneCommand
{
    public override string Command => "stopMission";
}

public sealed class ResumeMissionCommand : DroneCommand
{
    public override string Command => "startMission";
}
