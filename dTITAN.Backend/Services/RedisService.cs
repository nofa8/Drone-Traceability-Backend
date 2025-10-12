using StackExchange.Redis;
using System.Text.Json;

namespace dTITAN.Backend.Services;

public class RedisService
{
    private readonly ConnectionMultiplexer _conn;

    public RedisService(IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Redis");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Redis connection string not configured.");

        _conn = ConnectionMultiplexer.Connect(connectionString);
    }

    public IDatabase Db => _conn.GetDatabase();

    public void SetDronePosition(string id, object drone)
    {
        var json = JsonSerializer.Serialize(drone);
        Db.StringSet($"drone:{id}", json);
    }

    public string? GetDronePosition(string id)
    {
        RedisValue pos = Db.StringGet($"drone:{id}");
        return pos.HasValue ? pos.ToString() : null;
    }
}

