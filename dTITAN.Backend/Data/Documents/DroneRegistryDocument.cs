using MongoDB.Bson;

namespace dTITAN.Backend.Data.Documents;

public class Location
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public static Location FromDto(DTO.Location dto)
        => new()
        {
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };

    public bool Equals(Location other)
    {
        return other != null &&
               Latitude == other.Latitude &&
               Longitude == other.Longitude;
    }
}

public class DroneRegistryDocument
{
    public ObjectId _id { get; set; }
    public string DroneId { get; set; } = default!;
    public string Model { get; set; } = default!;
    public Location HomeLocation { get; set; } = new();

    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }

    public bool Equals(DroneRegistryDocument other)
    {
        return other != null &&
               DroneId == other.DroneId &&
               Model == other.Model &&
               HomeLocation.Equals(other.HomeLocation);
    }
}
