using System.Text.Json;
using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Transport.Websockets;

public sealed class ExternalEnvelope
{
    // UserId = Drone Id
    public string UserId { get; set; } = default!;

    public string? Role { get; set; }

    public JsonElement Message { get; set; }
    
    // XXX: Unknown purpose
    // [JsonPropertyName("store")]
    // public object? Store { get; set; }
}
