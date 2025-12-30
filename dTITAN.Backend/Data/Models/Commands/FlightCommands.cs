namespace dTITAN.Backend.Data.Models.Commands;

public class FlightCommand(string command) : DroneCommand, IHasAllowedCommands
{
    public override string Command => command;
    private static readonly List<string> allowedCommands =
    [
        "takeoff",
        "land",
        "startGoHome",
        "pauseMission",
        "stopMission",
        "startMission"
    ];

    public static IReadOnlyList<string> AllowedCommands => allowedCommands;

}
