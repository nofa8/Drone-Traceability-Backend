using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Models.Commands.Enums;

/// <summary>
/// Action to perform when a mission completes.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EndAction
{
    /// <summary>
    /// Land at the final waypoint.
    /// </summary>
    [JsonStringEnumMemberName("land")]
    Land,

    /// <summary>
    /// Return to home position and land.
    /// </summary>
    [JsonStringEnumMemberName("goHome")]
    GoHome,

    /// <summary>
    /// Do not perform any end action; hold position.
    /// </summary>
    [JsonStringEnumMemberName("none")]
    None
}