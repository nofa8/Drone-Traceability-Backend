using System;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace dTITAN.Backend.Middleware;

public class ClientWebSocketMiddleware
{
    private readonly RequestDelegate _next;

    public ClientWebSocketMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            await _next(context);
            return;
        }

        var socket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("Client connected to dummy WS endpoint.");

        var buffer = new byte[1024];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;
                // ignore incoming data for dummy endpoint
            }
        }
        finally
        {
            if (socket.State != WebSocketState.Closed)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            Console.WriteLine("Client disconnected from dummy WS endpoint.");
        }
    }

}