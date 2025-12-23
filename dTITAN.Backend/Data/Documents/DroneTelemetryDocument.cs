using dTITAN.Backend.Data.Events;
using MongoDB.Bson;

namespace dTITAN.Backend.Data.Documents;

public class DroneTelemetryDocument
{
    public ObjectId _id { get; set; }
    public string DroneId { get; set; } = default!;
    public DateTime Timestamp { get; set; }

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
    
    public static DroneTelemetryDocument FromEvent(DroneTelemetryReceived evt)
    {
        var d = evt.Drone;
        return new DroneTelemetryDocument
        {
            DroneId = d.Id,
            Timestamp = evt.ReceivedAt,
            Latitude = d.Latitude,
            Longitude = d.Longitude,
            Altitude = d.Altitude,
            VelocityX = d.VelocityX,
            VelocityY = d.VelocityY,
            VelocityZ = d.VelocityZ,
            BatteryLevel = d.BatteryLevel,
            BatteryTemperature = d.BatteryTemperature,
            Heading = d.Heading,
            SatelliteCount = d.SatelliteCount,
            RemainingFlightTime = d.RemainingFlightTime,
            IsTraveling = d.IsTraveling,
            IsFlying = d.IsFlying,
            Online = d.Online,
            IsGoingHome = d.IsGoingHome,
            IsHomeLocationSet = d.IsHomeLocationSet,
            AreMotorsOn = d.AreMotorsOn,
            AreLightsOn = d.AreLightsOn
        };
    }


}
