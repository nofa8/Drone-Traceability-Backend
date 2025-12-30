namespace dTITAN.Backend.Data.Models.Commands;

public interface IHasAllowedCommands
{
    static abstract IReadOnlyList<string> AllowedCommands { get; }
}
