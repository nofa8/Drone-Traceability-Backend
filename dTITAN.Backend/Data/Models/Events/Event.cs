namespace dTITAN.Backend.Data.Models.Events;

public interface IEvent
{
    DateTime TimeStamp { get; }
    string EventType { get; }
}

public interface IBroadcastEvent : IEvent { }

public interface IConnectionEvent : IEvent
{
    ConnectionId Target { get; }
}

public interface IDroneEvent: IBroadcastEvent{ }