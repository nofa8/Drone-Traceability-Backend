using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Websockets.Commands;

/// <summary>
/// Command to arm or disarm the motors.
/// </summary>
public sealed class MotorsCommand : DroneCommand
{
    public override string Command => "motors";

    /// <summary>
    /// Set <c>true</c> to arm/start motors, <c>false</c> to disarm/stop motors.
    /// </summary>
    [JsonPropertyName("state")]
    public bool State { get; set; }
}


/// <summary>
/// Command to toggle identification lights on the vehicle.
/// </summary>
public sealed class IdentificationCommand : DroneCommand
{
    public override string Command => "identify";

    /// <summary>
    /// Set <c>true</c> to activate identification lights, <c>false</c> to deactivate.
    /// </summary>
    [JsonPropertyName("state")]
    public bool State { get; set; }
}