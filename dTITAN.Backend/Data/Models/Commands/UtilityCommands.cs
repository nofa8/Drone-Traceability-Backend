namespace dTITAN.Backend.Data.Models.Commands;

public class UtilityCommand(string command) : DroneCommand, IHasAllowedCommands
{
    public static IReadOnlyList<string> AllowedCommands =>
    [
        "motors",
        "identify",
        "virtualSticks",
    ];

    public bool State { get; set; }

    public override string Command => command;
}