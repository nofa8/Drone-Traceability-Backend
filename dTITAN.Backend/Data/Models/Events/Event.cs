namespace dTITAN.Backend.Data.Models.Events;

public interface IEvent
{
    DateTime TimeStamp { get; }
    string EventType { get; }

    /// <summary>
    /// Returns the object that should be sent as Payload over WebSocket.
    /// </summary>
    public object ToPayload();
}

public interface IBroadcastEvent : IEvent { }

public interface IConnectionEvent : IEvent
{
    ConnectionId Target { get; }
}

public interface IDroneEvent : IBroadcastEvent { }