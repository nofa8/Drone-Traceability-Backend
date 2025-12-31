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

public interface ICommandEvent : IEvent { }

public interface IPublicEvent : IEvent { }
public interface IBroadcastEvent : IPublicEvent { }
public interface IDroneEvent : IBroadcastEvent { }

public interface IConnectionEvent : IPublicEvent { }
