using System.Text.Json;
using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Transport.Websockets;

public sealed class ExternalEnvelopeWs
{
    // UserId = Drone Id
    [JsonPropertyName("userId")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("message")]
    public JsonElement Message { get; set; }
    
    // XXX: Unknown purpose
    // [JsonPropertyName("store")]
    // public object? Store { get; set; }
}
