using System;
using System.Text.Json;
using MongoDB.Driver;
using dTITAN.Backend.Models;
using dTITAN.Backend.Data;

namespace dTITAN.Backend.Services;

public interface IDroneService
{
    Task<Drone> AddDroneAsync(Drone drone);
    Task<Drone?> GetDroneAsync(string id);
    Task<List<string>> GetAllDronesAsync();
}

public class DroneService(MongoDbContext mongoContext, RedisService redis) : IDroneService
{
    private IMongoCollection<Drone> Drones => mongoContext.GetCollection<Drone>("Drones");

    // TODO: Update DTO params
    public async Task<Drone> AddDroneAsync(Drone drone)
    {
        await Drones.InsertOneAsync(drone);
        redis.SetDronePosition(drone.Id, drone);

        return drone;
    }

    public async Task<Drone?> GetDroneAsync(string id)
    {
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

    public async Task<List<string>> GetAllDronesAsync()
    {
        var drones = await Drones.Find(_ => true).ToListAsync();
        return drones.Select(d => d.Id).ToList();
    }
}