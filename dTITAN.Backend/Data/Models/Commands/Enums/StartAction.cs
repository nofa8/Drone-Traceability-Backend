using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Transport.Websockets.Enums;

/// <summary>
/// Action to perform before a mission starts.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StartAction
{
    /// <summary>
    /// Automatically take off before the mission begins.
    /// </summary>
    [JsonStringEnumMemberName("takeoff")]
    Takeoff,

    /// <summary>
    /// Do not perform any start action; begin from current state.
    /// </summary>
    [JsonStringEnumMemberName("none")]
    None
}