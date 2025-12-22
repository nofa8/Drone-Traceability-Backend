using System;
using System.Text.Json;
using MongoDB.Driver;
using dTITAN.Backend.Models;
using dTITAN.Backend.Data;
using Microsoft.Extensions.Logging;

namespace dTITAN.Backend.Services;

public interface IDroneService
{
    /// <summary>
    /// Adds a new drone to persistent storage and returns the stored entity.
    /// </summary>
    /// <param name="drone">Drone instance to add.</param>
    /// <returns>The stored <see cref="Drone"/>.</returns>
    Task<Drone> AddDroneAsync(Drone drone);

    /// <summary>
    /// Retrieves a drone by id. May return <c>null</c> if not found.
    /// </summary>
    /// <param name="id">Identifier of the drone.</param>
    /// <returns>The <see cref="Drone"/> or <c>null</c> when not present.</returns>
    Task<Drone?> GetDroneAsync(string id);

    /// <summary>
    /// Returns a list of all known drone ids.
    /// </summary>
    /// <returns>List of drone identifiers.</returns>
    Task<List<string>> GetAllDronesAsync();
}

/// <summary>
/// Service encapsulating storage and retrieval operations for <see cref="Drone"/> instances.
/// Uses MongoDB for persistent storage and Redis for quick position lookups.
/// </summary>
/// <param name="mongoContext">MongoDB context for collection access.</param>
/// <param name="redis">Redis helper used for caching drone positions.</param>
/// <param name="logger">Logger instance used for operational logging.</param>
public class DroneService(MongoDbContext mongoContext, RedisService redis, ILogger<DroneService> logger) : IDroneService
{
    private readonly ILogger<DroneService> _logger = logger;
    private IMongoCollection<Drone> Drones => mongoContext.GetCollection<Drone>("Drones");

    /// <summary>
    /// Persists a new drone and updates the Redis cache with the current position.
    /// </summary>
    /// <param name="drone">Drone to persist.</param>
    /// <returns>The persisted <see cref="Drone"/>.</returns>
    public async Task<Drone> AddDroneAsync(Drone drone)
    {
        _logger.LogInformation("Adding drone {DroneId}", drone.Id);
        await Drones.InsertOneAsync(drone);
        redis.SetDronePosition(drone.Id, drone);

        return drone;
    }

    /// <summary>
    /// Gets a drone by id. Attempts a Redis lookup first, then falls back to MongoDB.
    /// </summary>
    /// <param name="id">Drone identifier.</param>
    /// <returns>The <see cref="Drone"/> or <c>null</c> if not found.</returns>
    public async Task<Drone?> GetDroneAsync(string id)
    {
        _logger.LogDebug("Getting drone {DroneId}", id);
        var redisValue = redis.GetDronePosition(id);
        if (!string.IsNullOrEmpty(redisValue))
        {
            var drone = JsonSerializer.Deserialize<Drone>(redisValue);
            if (drone != null)
                return drone;
        }

        var droneDb = await Drones.Find(d => d.Id == id).FirstOrDefaultAsync();
        if (droneDb != null)
        {
            return droneDb;
        }

        return null;
    }

    /// <summary>
    /// Loads all drones from MongoDB and returns their identifiers.
    /// </summary>
    /// <returns>List of drone ids.</returns>
    public async Task<List<string>> GetAllDronesAsync()
    {
        _logger.LogDebug("Loading all drone ids");
        var drones = await Drones.Find(_ => true).ToListAsync();
        return drones.Select(d => d.Id).ToList();
    }
}