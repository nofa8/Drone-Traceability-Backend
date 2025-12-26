using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Transport.Websockets;

public sealed class WsEnvelope<TMessage>
{
    [JsonPropertyName("userId")]
    public string Id { get; set; } = default!; // UserId, Drone Id

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("message")]
    public TMessage Message { get; set; } = default!;

    [JsonPropertyName("store")]
    public object? Store { get; set; } // Unknown purpose
}
