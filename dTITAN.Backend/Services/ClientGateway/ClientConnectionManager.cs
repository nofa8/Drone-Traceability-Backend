using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Models.Events;
using dTITAN.Backend.Data.Transport.Websockets;
using dTITAN.Backend.Services.EventBus;

namespace dTITAN.Backend.Services.ClientGateway;

public class ClientConnectionManager
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ClientConnectionManager> _logger;
    private readonly ConcurrentDictionary<ConnectionId, WebSocket> _clients;

    public ClientConnectionManager(IEventBus eventBus, ILogger<ClientConnectionManager> logger)
    {
        _clients = new();
        _eventBus = eventBus;
        _logger = logger;
        _eventBus.Subscribe<IEvent>(HandleEvent);
    }

    public ConnectionId AddClient(WebSocket socket)
    {
        var id = new ConnectionId(Guid.NewGuid());
        _clients[id] = socket;
        return id;
    }
    public async Task RemoveClient(ConnectionId id)
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
            IConnectionEvent c => SendToConnection(c.Target, c),
            _ => Task.CompletedTask
        };

    private async Task SendToConnection(ConnectionId target, IConnectionEvent evt)
    {
        if (!_clients.TryGetValue(target, out var socket)) return;
        if (socket.State != WebSocketState.Open)
        {
            await RemoveClient(target);
            return;
        }
        // XXX: Send command event
        await Send(socket, EventEnvelope.From(evt));
    }

    private async Task BroadcastToAll(IBroadcastEvent evt)
    {
        var dead = new List<ConnectionId>();
        foreach (var (id, socket) in _clients)
        {
            if (socket.State != WebSocketState.Open)
            {
                dead.Add(id);
                continue;
            }
            await Send(socket, EventEnvelope.From(evt));
        }
        foreach (var id in dead) await RemoveClient(id);
    }

    private static async Task Send(WebSocket socket, EventEnvelope eventEnvelope)
    {
        var json = JsonSerializer.Serialize(eventEnvelope);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
