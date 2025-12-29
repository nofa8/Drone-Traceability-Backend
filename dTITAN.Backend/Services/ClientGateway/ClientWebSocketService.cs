using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using dTITAN.Backend.Data.Models;

namespace dTITAN.Backend.Services.ClientGateway;

public class ClientWebSocketService : IAsyncDisposable
{
    private readonly ILogger<ClientWebSocketService> _logger;
    private readonly ClientConnectionManager _manager;
    private readonly Channel<(ConnectionId id, string message)> _messageChannel;
    private readonly Task _processTask;

    public ClientWebSocketService(ClientConnectionManager manager, ILogger<ClientWebSocketService> logger)
    {
        _logger = logger;
        _manager = manager;
        _messageChannel = Channel.CreateUnbounded<(ConnectionId, string)>();
        
        // Start background processor
        _processTask = Task.Run(ProcessMessages);
    }

    public async Task HandleClientAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var id = _manager.AddClient(socket);

        await ReceiveLoop(socket, id);

        await _manager.RemoveClient(id);
    }

    private async Task ReceiveLoop(WebSocket socket, ConnectionId id)
    {
        var buffer = new ArraySegment<byte>(new byte[8192]);

        while (socket.State == WebSocketState.Open)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
                    return;
                }
                ms.Write(buffer.Array!, buffer.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            if (ms.Length == 0) continue;
            var text = Encoding.UTF8.GetString(ms.ToArray());
            await _messageChannel.Writer.WriteAsync((id, text));
        }
    }

    private async Task ProcessMessages()
    {
        await foreach (var (id, message) in _messageChannel.Reader.ReadAllAsync())
        {
            // XXX: check this when doing the drone commands
            // Just pass the raw message to the manager or event bus
            // No deserialization or processing here unless needed
        }
    }

    public async ValueTask DisposeAsync()
    {
        _messageChannel.Writer.TryComplete();
        try
        {
            await _processTask;
        }
        catch
        {
            _logger.LogError("Error disposing ClientWebSocketService");
        }
    }
}

