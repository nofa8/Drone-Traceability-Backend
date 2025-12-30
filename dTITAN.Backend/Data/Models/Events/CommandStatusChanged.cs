namespace dTITAN.Backend.Data.Models.Events;



public record CommandStatusChanged(CommandStatus Status, DateTime TimeStamp) : ICommandEvent
{
    public string EventType => nameof(CommandStatusChanged);
    public object ToPayload() => Status;
}