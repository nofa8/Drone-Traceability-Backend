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

public class DroneService(MongoDbContext mongoContext, ILogger<DroneService> logger) : IDroneService
{
    private readonly ILogger<DroneService> _logger = logger;
    private IMongoCollection<Drone> Drones => mongoContext.GetCollection<Drone>("Drones");

    public async Task<Drone> AddDroneAsync(Drone drone)
    {
        _logger.LogInformation("Adding drone {DroneId}", drone.Id);
        await Drones.InsertOneAsync(drone);

        return drone;
    }

    public async Task<Drone?> GetDroneAsync(string id)
    {
        _logger.LogDebug("Getting drone {DroneId}", id);
        var droneDb = await Drones.Find(d => d.Id == id).FirstOrDefaultAsync();
        if (droneDb != null)
        {
            return droneDb;
        }

        return null;
    }

    public async Task<List<string>> GetAllDronesAsync()
    {
        _logger.LogDebug("Loading all drone ids");
        var drones = await Drones.Find(_ => true).ToListAsync();
        var ids = drones.Select(d => d.Id).ToList();
        _logger.LogInformation("Loaded {Count} drones from DB", ids.Count);
        return ids;
    }
}