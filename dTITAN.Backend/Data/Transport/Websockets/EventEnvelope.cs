using dTITAN.Backend.Data.Models.Events;

namespace dTITAN.Backend.Data.Transport.Websockets;

public sealed class EventEnvelope
{
    public DateTime TimeStamp { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public object Payload { get; set; } = default!;

    public static EventEnvelope From(IEvent evt) => new()
    {
        TimeStamp = evt.TimeStamp,
        EventType = evt.EventType,
        Payload = evt.ToPayload()
    };
}
