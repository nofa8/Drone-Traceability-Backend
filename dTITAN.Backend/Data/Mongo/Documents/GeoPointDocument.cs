using dTITAN.Backend.Data.Models;

namespace dTITAN.Backend.Data.Mongo.Documents;

public class GeoPointDocument : IEquatable<GeoPoint>
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public bool Equals(GeoPoint? other)
    {
        return other != null &&
               Latitude == other.Latitude &&
               Longitude == other.Longitude;
    }
}