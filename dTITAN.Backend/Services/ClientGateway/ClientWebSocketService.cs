using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using dTITAN.Backend.Data.Models;

namespace dTITAN.Backend.Services.ClientGateway;

public class ClientWebSocketService(ClientConnectionManager manager, Channel<(Guid, string)> messageChannel, ILogger<ClientWebSocketService> logger)
{
    private readonly ILogger<ClientWebSocketService> _logger = logger;
    private readonly ClientConnectionManager _manager = manager;
    private readonly Channel<(Guid id, string message)> _messageChannel = messageChannel;

    public async Task HandleClientAsync(HttpContext context, CancellationToken appStopping)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var id = _manager.AddClient(socket);
        _logger.LogInformation("Client connected: {id}", id);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            appStopping);

        await ReceiveLoop(socket, id, linkedCts.Token);

        await _manager.RemoveClient(id);
    }

    private async Task ReceiveLoop(WebSocket socket, Guid id, CancellationToken cancellationToken)
    {
        var buffer = new ArraySegment<byte>(new byte[8192]);
        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", cancellationToken);
                        return;
                    }
                    ms.Write(buffer.Array!, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                if (ms.Length == 0) continue;
                var text = Encoding.UTF8.GetString(ms.ToArray());
                await _messageChannel.Writer.WriteAsync((id, text), cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException)
        {
            _logger.LogWarning(message: "WebSocket disconnected unexpectedly: {id}", id);
        }
        finally
        {
            socket.Abort();
        }
    }
}

