using dTITAN.Backend.Data.Transport.Websockets;

namespace dTITAN.Backend.Data.Models;

public class GeoPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public static GeoPoint From(GeoPointWs ws) => new()
    {
        Latitude = ws.Latitude,
        Longitude = ws.Longitude
    };

    public bool Equals(GeoPoint other)
    {
        return other != null &&
               Latitude == other.Latitude &&
               Longitude == other.Longitude;
    }
}