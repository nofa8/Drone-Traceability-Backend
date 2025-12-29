using System.Text.Json.Serialization;

namespace dTITAN.Backend.Data.Transport.Websockets;

public class GeoPointWs
{
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double Longitude { get; set; }

    public bool Equals(GeoPointWs other)
    {
        return other != null &&
               Latitude == other.Latitude &&
               Longitude == other.Longitude;
    }
}