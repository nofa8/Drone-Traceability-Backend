using System;
using System.Text.Json.Serialization;

namespace dTITAN.Backend.Models;

public class Location2
{
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double Longitude { get; set; }
}

public class Drone2
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double Longitude { get; set; }

    [JsonPropertyName("homeLocation")]
    public Location HomeLocation { get; set; } = new Location();

    [JsonPropertyName("alt")]
    public double Altitude { get; set; }

    [JsonPropertyName("velX")]
    public double VelocityX { get; set; }

    [JsonPropertyName("velY")]
    public double VelocityY { get; set; }

    [JsonPropertyName("velZ")]
    public double VelocityZ { get; set; }

    [JsonPropertyName("batLvl")]
    public int BatteryLevel { get; set; }

    [JsonPropertyName("batTemperature")]
    public double BatteryTemperature { get; set; }

    [JsonPropertyName("hdg")]
    public double Heading { get; set; }

    [JsonPropertyName("satCount")]
    public int SatelliteCount { get; set; }

    [JsonPropertyName("rft")]
    public int RFT { get; set; }

    [JsonPropertyName("isTraveling")]
    public bool IsTraveling { get; set; }

    [JsonPropertyName("isFlying")]
    public bool IsFlying { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("online")]
    public bool Online { get; set; }

    [JsonPropertyName("isGoingHome")]
    public bool IsGoingHome { get; set; }

    [JsonPropertyName("isHomeLocationSet")]
    public bool IsHomeLocationSet { get; set; }

    [JsonPropertyName("areMotorsOn")]
    public bool AreMotorsOn { get; set; }

    [JsonPropertyName("areLightsOn")]
    public bool AreLightsOn { get; set; }
}
