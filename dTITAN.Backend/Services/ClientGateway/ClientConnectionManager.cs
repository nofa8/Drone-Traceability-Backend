using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using dTITAN.Backend.Data.Models.Events;
using dTITAN.Backend.Data.Transport.Websockets;
using dTITAN.Backend.Services.EventBus;

namespace dTITAN.Backend.Services.ClientGateway;

public class ClientConnectionManager
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ClientConnectionManager> _logger;
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ClientConnectionManager(IEventBus eventBus, ILogger<ClientConnectionManager> logger)
    {
        _clients = new();
        _eventBus = eventBus;
        _logger = logger;
        _eventBus.Subscribe<IPublicEvent>(HandleEvent);
    }

    public Guid AddClient(WebSocket socket)
    {
        var id = Guid.NewGuid();
        _clients[id] = socket;
        return id;
    }

    public async Task RemoveClient(Guid id)
    {
        if (_clients.TryRemove(id, out var socket))
        {
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                                        "Connection closed",
                                        CancellationToken.None);
            }
            socket.Dispose();
        }
    }

    private Task HandleEvent(IEvent evt) =>
        evt switch
        {
            IBroadcastEvent b => BroadcastToAll(b),
            // XXX: no events like this yet 
            // IConnectionEvent c => SendToConnection(c),
            _ => Task.CompletedTask
        };

    private Task SendToConnection(IConnectionEvent evt)
    {
        _logger.LogWarning("Received IConnectionEvent of type {EventType}, but SendToConnection is not implemented. The event will be ignored.", evt.GetType().Name);
        return Task.CompletedTask;
    }

    private async Task BroadcastToAll(IBroadcastEvent evt)
    {
        var dead = new List<Guid>();
        foreach (var (id, socket) in _clients)
        {
            if (socket.State != WebSocketState.Open)
            {
                dead.Add(id);
                continue;
            }
            await Send(id, socket, EventEnvelope.From(evt));
        }
        foreach (var id in dead) await RemoveClient(id);
    }

    private async Task Send(Guid id, WebSocket socket, EventEnvelope eventEnvelope)
    {
        byte[] bytes;
        try
        {
            var json = JsonSerializer.Serialize(eventEnvelope, _jsonOptions);
            bytes = Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize event envelope for WebSocket transmission.");
            return;
        }
        try
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception)
        {
            _logger.LogWarning("Failed to send event to WebSocket client {ClientId}. Removing client.", id);
            await RemoveClient(id);
        }
    }
}
