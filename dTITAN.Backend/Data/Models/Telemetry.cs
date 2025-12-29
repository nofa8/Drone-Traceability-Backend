using dTITAN.Backend.Data.Persistence;
using dTITAN.Backend.Data.Transport.Websockets;

namespace dTITAN.Backend.Data.Models;

public class Telemetry
{
    public DateTime Timestamp { get; init; }

    public GeoPoint HomeLocation { get; set; } = default!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }

    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
    public double VelocityZ { get; set; }

    public double BatteryLevel { get; set; }
    public double BatteryTemperature { get; set; }

    public double Heading { get; set; }
    public int SatelliteCount { get; set; }
    public int RemainingFlightTime { get; set; }

    public bool IsTraveling { get; set; }
    public bool IsFlying { get; set; }
    public bool Online { get; set; }
    public bool IsGoingHome { get; set; }
    public bool IsHomeLocationSet { get; set; }
    public bool AreMotorsOn { get; set; }
    public bool AreLightsOn { get; set; }

    public static Telemetry From(DroneTelemetryWs ws, DateTime timestamp) => new()
    {
        Timestamp = timestamp,
        HomeLocation = GeoPoint.From(ws.HomeLocation),
        Latitude = ws.Latitude,
        Longitude = ws.Longitude,
        Altitude = ws.Altitude,
        VelocityX = ws.VelocityX,
        VelocityY = ws.VelocityY,
        VelocityZ = ws.VelocityZ,
        BatteryLevel = ws.BatteryLevel,
        BatteryTemperature = ws.BatteryTemperature,
        Heading = ws.Heading,
        SatelliteCount = ws.SatelliteCount,
        RemainingFlightTime = ws.RemainingFlightTime,
        IsTraveling = ws.IsTraveling,
        IsFlying = ws.IsFlying,
        Online = ws.Online,
        IsGoingHome = ws.IsGoingHome,
        IsHomeLocationSet = ws.IsHomeLocationSet,
        AreMotorsOn = ws.AreMotorsOn,
        AreLightsOn = ws.AreLightsOn
    };
    
    public static Telemetry From(DroneTelemetryDocument doc) => new()
    {
        Timestamp = doc.Timestamp,
        HomeLocation = doc.HomeLocation,
        Latitude = doc.Latitude,
        Longitude = doc.Longitude,
        Altitude = doc.Altitude,
        VelocityX = doc.VelocityX,
        VelocityY = doc.VelocityY,
        VelocityZ = doc.VelocityZ,
        BatteryLevel = doc.BatteryLevel,
        BatteryTemperature = doc.BatteryTemperature,
        Heading = doc.Heading,
        SatelliteCount = doc.SatelliteCount,
        RemainingFlightTime = doc.RemainingFlightTime,
        IsTraveling = doc.IsTraveling,
        IsFlying = doc.IsFlying,
        Online = doc.Online,
        IsGoingHome = doc.IsGoingHome,
        IsHomeLocationSet = doc.IsHomeLocationSet,
        AreMotorsOn = doc.AreMotorsOn,
        AreLightsOn = doc.AreLightsOn
    };
}