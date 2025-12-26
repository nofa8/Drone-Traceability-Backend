namespace dTITAN.Backend.Data.Websockets.Commands;

/// <summary>
/// Command to initiate an automated takeoff sequence.
/// </summary>
public sealed class TakeoffCommand : DroneCommand
{
    public override string Command => "takeoff";
}

/// <summary>
/// Command to initiate an automated landing sequence.
/// </summary>
public sealed class LandCommand : DroneCommand
{
    public override string Command => "land";
}

/// <summary>
/// Command to return the vehicle to its home position and land.
/// </summary>
public sealed class GoHomeCommand : DroneCommand
{
    public override string Command => "startGoHome";
}
