using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Transport.Websockets.Commands;

/// <summary>
/// Base type for WebSocket drone commands. Derived types provide payload
/// properties and the concrete command name.
/// </summary>
public abstract class DroneCommand
{
    [JsonPropertyName("command")]
    public abstract string Command { get; }
}

