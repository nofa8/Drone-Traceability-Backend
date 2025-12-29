using System.Text.Json;
using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Transport.Websockets;

public sealed class ClientEnvelopeWs
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("message")]
    public required JsonElement Message { get; set; }
}