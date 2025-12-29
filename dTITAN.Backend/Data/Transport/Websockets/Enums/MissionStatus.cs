using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Transport.Websockets.Enums;

/**
 * XXX:
 * This looks like a info that is recieved and not sent.
 * Check return info of mission
 */
[JsonConverter(typeof(JsonStringEnumConverter))]
/// <summary>
/// Current execution status of a mission.
/// </summary>
public enum MissionStatus
{
    /// <summary>
    /// Mission is actively executing.
    /// </summary>
    [JsonPropertyName("RUNNING")]
    Running,

    /// <summary>
    /// Mission is temporarily halted.
    /// </summary>
    [JsonPropertyName("PAUSED")]
    Paused,

    /// <summary>
    /// Mission finished successfully.
    /// </summary>
    [JsonPropertyName("COMPLETED")]
    Completed,

    /// <summary>
    /// Mission aborted by user command.
    /// </summary>
    [JsonPropertyName("STOPPED")]
    Stopped,
}
