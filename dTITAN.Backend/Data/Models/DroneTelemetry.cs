using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Models;

public class DroneTelemetry
{
    // Drone Metadata
    [JsonPropertyName("id")]
    required public string Id { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    // Drone State
    [JsonPropertyName("homeLocation")]
    public GeoPoint HomeLocation { get; set; } = new GeoPoint();
    
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double Longitude { get; set; }

    [JsonPropertyName("alt")]
    public double Altitude { get; set; }

    [JsonPropertyName("velX")]
    public double VelocityX { get; set; }

    [JsonPropertyName("velY")]
    public double VelocityY { get; set; }

    [JsonPropertyName("velZ")]
    public double VelocityZ { get; set; }

    [JsonPropertyName("batLvl")]
    public double BatteryLevel { get; set; }

    [JsonPropertyName("batTemperature")]
    public double BatteryTemperature { get; set; }

    [JsonPropertyName("hdg")]
    public double Heading { get; set; }

    [JsonPropertyName("satCount")]
    public int SatelliteCount { get; set; }

    [JsonPropertyName("rft")]
    public int RemainingFlightTime { get; set; }

    [JsonPropertyName("isTraveling")]
    public bool IsTraveling { get; set; }

    [JsonPropertyName("isFlying")]
    public bool IsFlying { get; set; }

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
