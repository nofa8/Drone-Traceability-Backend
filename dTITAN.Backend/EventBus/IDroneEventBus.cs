using dTITAN.Backend.Events;

namespace dTITAN.Backend.EventBus;

public interface IDroneEventBus
{
    void Publish(IDroneEvent droneEvent);
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDroneEvent;
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDroneEvent;
}
