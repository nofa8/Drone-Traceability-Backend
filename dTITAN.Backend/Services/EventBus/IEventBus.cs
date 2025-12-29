using dTITAN.Backend.Data.Models.Events;

namespace dTITAN.Backend.Services.EventBus;

public interface IEventBus
{
    void Publish(IEvent droneEvent);
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent;
}
