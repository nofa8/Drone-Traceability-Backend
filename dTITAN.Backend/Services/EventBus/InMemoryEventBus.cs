using System.Collections.Concurrent;
using dTITAN.Backend.Data.Models.Events;

namespace dTITAN.Backend.Services.EventBus;

public class InMemoryEventBus(ILogger<InMemoryEventBus> logger) : IEventBus
{
    private readonly ILogger<InMemoryEventBus> _logger = logger;
    private readonly ConcurrentDictionary<Type, ConcurrentBag<Func<IEvent, Task>>> _handlers = new();


    private async Task SafeInvokeAsync(Func<IEvent, Task> handler, IEvent evt)
    {
        try { await handler(evt); }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Handler error processing event {EventType}", evt?.GetType()?.Name);
        }
    }

    public void Publish(IEvent evt)
    {
        var eventType = evt.GetType();
        _logger?.LogDebug("Publishing event {EventType}", eventType.Name);

        foreach (var (key, handlers) in _handlers)
        {
            if (!key.IsAssignableFrom(eventType)) continue;

            // XXX: Fire-and-forget, handlers are expected to handle their own errors
            foreach (var handler in handlers.ToArray())
                _ = SafeInvokeAsync(handler, evt);
        }
    }


    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        var type = typeof(TEvent);
        Task wrapper(IEvent e) => handler((TEvent)e);
        var bag = _handlers.GetOrAdd(type, _ => []);
        bag.Add(wrapper);
        _logger?.LogDebug("Subscribed handler for event {EventType}. Total handlers: {Count}", type.Name, bag.Count);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        throw new NotImplementedException("Unsubscribe is not implemented in InMemoryDroneEventBus");
    }
}
