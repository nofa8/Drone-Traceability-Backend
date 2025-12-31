namespace dTITAN.Backend.Data.Models.Commands;

public class FlightCommand : DroneCommand, IHasAllowedCommands
{
    public FlightCommand(string command) { Command = command; }

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
