namespace dTITAN.Backend.Data.Models.Commands;

public class UtilityCommand : DroneCommand, IHasAllowedCommands
{
    public UtilityCommand(string command)
    {
        Command = command;
    }
    public static IReadOnlyList<string> AllowedCommands =>
    [
        "motors",
        "identify",
        "virtualSticks",
    ];

    public bool State { get; set; }
}