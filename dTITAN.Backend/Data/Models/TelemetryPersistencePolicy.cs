namespace dTITAN.Backend.Data.Models;

public static class TelemetryPersistencePolicy
{
    private const double _MinHorizontalDistanceMeters = 1.5;
    private const double _MinAltitudeDeltaMeters = 0.5;
    private const double _MinVelocityDeltaMetersPerSecond = 0.3;
    private const double _MinHeadingDeltaDegrees = 5.0;
    private const double _MinBatteryLevelDeltaPercent = 1.0;
    private const double _MinBatteryTempDeltaCelsius = 1.0;
    private static readonly TimeSpan _MaxPersistenceInterval = TimeSpan.FromSeconds(5);

    public static bool ShouldPersist(
        Telemetry current,
        TelemetryPersistenceState? previous,
        DateTime now)
    {
        if (previous is null) return true;

        var last = previous.LastPersisted;

        if (HasBooleanStateChanged(current, last)) return true;

        // Movement changes 
        if (HasMovedSignificantly(current, last)) return true;
        if (Math.Abs(current.Altitude - last.Altitude) >= _MinAltitudeDeltaMeters) return true;
        if (VelocityDelta(current, last) >= _MinVelocityDeltaMetersPerSecond) return true;
        if (AngularDelta(current.Heading, last.Heading) >= _MinHeadingDeltaDegrees) return true;

        // Battery changes
        if (Math.Abs(current.BatteryLevel - last.BatteryLevel) >= _MinBatteryLevelDeltaPercent) return true;
        if (Math.Abs(current.BatteryTemperature - last.BatteryTemperature) >= _MinBatteryTempDeltaCelsius) return true;

        if (current.SatelliteCount != last.SatelliteCount) return true;

        if (CrossedRemainingTimeBoundary(current, last)) return true;

        // Time-based safety net
        if (now - previous.LastPersistedAt >= _MaxPersistenceInterval) return true;

        return false;
    }

    private static bool HasBooleanStateChanged(Telemetry a, Telemetry b) =>
        a.IsTraveling != b.IsTraveling ||
        a.IsFlying != b.IsFlying ||
        a.Online != b.Online ||
        a.IsGoingHome != b.IsGoingHome ||
        a.IsHomeLocationSet != b.IsHomeLocationSet ||
        a.AreMotorsOn != b.AreMotorsOn ||
        a.AreLightsOn != b.AreLightsOn;

    private static bool HasMovedSignificantly(Telemetry a, Telemetry b)
    {
        var distance = HaversineMeters(a.Latitude, a.Longitude, b.Latitude, b.Longitude);
        return distance >= _MinHorizontalDistanceMeters;
    }

    private static double HaversineMeters(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        const double R = 6371000.0; // Earth radius (m)

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) *
            Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;

    private static double VelocityDelta(Telemetry a, Telemetry b)
    {
        var dx = a.VelocityX - b.VelocityX;
        var dy = a.VelocityY - b.VelocityY;
        var dz = a.VelocityZ - b.VelocityZ;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static double AngularDelta(double a, double b)
    {
        var delta = Math.Abs(a - b) % 360.0;
        return delta > 180.0 ? 360.0 - delta : delta;
    }

    private static bool CrossedRemainingTimeBoundary(Telemetry a, Telemetry b)
    {
        return
            (b.RemainingFlightTime > 300 && a.RemainingFlightTime <= 300) || // 5 min
            (b.RemainingFlightTime > 120 && a.RemainingFlightTime <= 120) || // 2 min
            (b.RemainingFlightTime > 60 && a.RemainingFlightTime <= 60);   // 1 min
    }
}
