using StackExchange.Redis;
using System.Text.Json;

namespace dTITAN.Backend.Services;

/// <summary>
/// Simple Redis-backed helper for caching drone state and providing a
/// convenient typed API around <see cref="StackExchange.Redis.ConnectionMultiplexer"/>.
/// </summary>
public class RedisService
{
    private readonly ConnectionMultiplexer _conn;
    private readonly ILogger<RedisService> _logger;

    /// <summary>
    /// Creates a new <see cref="RedisService"/> and establishes a connection
    /// to Redis using the configured connection string named "Redis".
    /// </summary>
    /// <param name="config">Application configuration containing connection strings.</param>
    /// <param name="logger">Logger used for connection diagnostics.</param>
    /// <exception cref="InvalidOperationException">Thrown if the Redis connection string is missing.</exception>
    public RedisService(IConfiguration config, ILogger<RedisService> logger)
    {
        _logger = logger;
        var connectionString = config.GetConnectionString("Redis");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Redis connection string not configured.");

        try
        {
            _conn = ConnectionMultiplexer.Connect(connectionString);
            _logger.LogInformation("Connected to Redis");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Redis");
            throw;
        }
    }

    /// <summary>
    /// Gets the Redis <see cref="IDatabase"/> instance used for operations.
    /// </summary>
    public IDatabase Db => _conn.GetDatabase();

    /// <summary>
    /// Stores the provided drone object into Redis as JSON under a drone-specific key.
    /// </summary>
    /// <param name="id">Drone identifier used as part of the Redis key.</param>
    /// <param name="drone">Object representing the drone state to serialize.</param>
    public void SetDronePosition(string id, object drone)
    {
        var json = JsonSerializer.Serialize(drone);
        _logger.LogDebug("Setting drone position in Redis for {DroneId}", id);
        Db.StringSet($"drone:{id}", json);
    }

    /// <summary>
    /// Retrieves the cached drone JSON for the given id, or <c>null</c> when no value exists.
    /// </summary>
    /// <param name="id">Drone identifier.</param>
    /// <returns>Serialized JSON string or <c>null</c> when absent.</returns>
    public string? GetDronePosition(string id)
    {
        _logger.LogDebug("Getting drone position from Redis for {DroneId}", id);
        RedisValue pos = Db.StringGet($"drone:{id}");
        return pos.HasValue ? pos.ToString() : null;
    }
}

