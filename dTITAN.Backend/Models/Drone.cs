using System;

namespace dTITAN.Backend.Models;

public class Location
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class Drone
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public double Lat { get; set; }
    public double Lng { get; set; }
    public Location HomeLocation { get; set; } = new Location();
    public double Alt { get; set; }
    public double VelX { get; set; }
    public double VelY { get; set; }
    public double VelZ { get; set; }
    public int BatLvl { get; set; }
    public double BatTemperature { get; set; }
    public double Hdg { get; set; }
    public int SatCount { get; set; }
    public int Rft { get; set; }
    public bool IsTraveling { get; set; }
    public bool IsFlying { get; set; }
    public string Model { get; set; } = "";
    public bool Online { get; set; }
    public bool IsGoingHome { get; set; }
    public bool IsHomeLocationSet { get; set; }
    public bool AreMotorsOn { get; set; }
    public bool AreLightsOn { get; set; }
}
