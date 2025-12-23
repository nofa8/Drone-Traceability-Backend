using System.Collections.Concurrent;
using dTITAN.Backend.Events;
using Microsoft.Extensions.Logging;

namespace dTITAN.Backend.EventBus;


public class InMemoryDroneEventBus(ILogger<InMemoryDroneEventBus> logger) : IDroneEventBus
{
    private readonly ILogger<InMemoryDroneEventBus> _logger = logger;
    private readonly ConcurrentDictionary<Type, ConcurrentBag<Func<IDroneEvent, Task>>> _handlers = new();


    private async Task SafeInvokeAsync(Func<IDroneEvent, Task> handler, IDroneEvent evt)
    {
        try { await handler(evt); }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Handler error processing event {EventType}", evt?.GetType()?.Name);
        }
    }
    public void Publish(IDroneEvent droneEvent)
    {
        var type = droneEvent.GetType();
        if (!_handlers.TryGetValue(type, out var handlers))
        {
            _logger?.LogDebug("Publishing event {EventType} but no handlers registered", type.Name);
            return;
        }

        var handlersSnapshot = handlers.ToArray();
        _logger?.LogDebug("Publishing event {EventType} to {HandlerCount} handlers", type.Name, handlersSnapshot.Length);
        foreach (var handler in handlersSnapshot)
        {
            // XXX: Fire-and-forget, handlers are expected to handle their own errors
            _ = SafeInvokeAsync(handler, droneEvent);
        }
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDroneEvent
    {
        var type = typeof(TEvent);
        Task wrapper(IDroneEvent e) => handler((TEvent)e);
        var bag = _handlers.GetOrAdd(type, _ => []);
        bag.Add(wrapper);
        _logger?.LogDebug("Subscribed handler for event {EventType}. Total handlers: {Count}", type.Name, bag.Count);
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDroneEvent
    {
        _logger?.LogWarning("Unsubscribe is not implemented");
    }
}
