using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Websockets.Commands.Enums;

/// <summary>
/// Action to perform when a mission completes.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EndAction
{
    /// <summary>
    /// Land at the final waypoint.
    /// </summary>
    [JsonPropertyName("land")]
    Land,

    /// <summary>
    /// Return to home position and land.
    /// </summary>
    [JsonPropertyName("goHome")]
    GoHome,

    /// <summary>
    /// Do not perform any end action; hold position.
    /// </summary>
    [JsonPropertyName("none")]
    None
}