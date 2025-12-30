namespace dTITAN.Backend.Data.Models.Events;



public record CommandReceived(DroneCommandContext Command, DateTime TimeStamp) : ICommandEvent
{
    public string EventType => nameof(CommandReceived);
    public object ToPayload() => Command.Command;
}