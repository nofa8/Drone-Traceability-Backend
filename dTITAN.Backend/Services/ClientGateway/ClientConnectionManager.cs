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
            IConnectionEvent c => SendToConnection(c),
            _ => Task.CompletedTask
        };

    private async Task SendToConnection(IConnectionEvent evt)
    {
        if (evt is not CommandStatusChanged)
        {
            _logger.LogWarning("Unsupported connection event type: {EventType}", evt.GetType().Name);
            return;
        }
        var target = ((CommandStatusChanged)evt).Status.ConnectionId;
        if (!_clients.TryGetValue(target, out var socket)) return;
        if (socket.State != WebSocketState.Open)
        {
            await RemoveClient(target);
            return;
        }
        await Send(socket, EventEnvelope.From(evt));
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
            await Send(socket, EventEnvelope.From(evt));
        }
        foreach (var id in dead) await RemoveClient(id);
    }

    private static async Task Send(WebSocket socket, EventEnvelope eventEnvelope)
    {
        var json = JsonSerializer.Serialize(eventEnvelope, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
