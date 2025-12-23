using MongoDB.Driver;
using dTITAN.Backend.EventBus;
using dTITAN.Backend.Data;
using dTITAN.Backend.Data.Documents;
using dTITAN.Backend.Data.Events;

namespace dTITAN.Backend.Services.Persistence;

public class DroneRegistryWriter
{
    private readonly IMongoCollection<DroneRegistryDocument> _collection;

    public DroneRegistryWriter(MongoDbContext db, IDroneEventBus eventBus)
    {
        _collection = db.GetCollection<DroneRegistryDocument>("drone_registry");
        eventBus.Subscribe<DroneConnected>(HandleDroneConnected);
        eventBus.Subscribe<DroneTelemetryReceived>(HandleTelemetryReceived);
        eventBus.Subscribe<DroneDisconnected>(HandleDroneDisconnected);
    }

    private async Task HandleDroneConnected(DroneConnected evt)
    {
        var filter = Builders<DroneRegistryDocument>.Filter.Eq(d => d.DroneId, evt.Drone.Id);
        var existing = await _collection.Find(filter).FirstOrDefaultAsync();

        if (existing == null)
        {
            var doc = new DroneRegistryDocument
            {
                DroneId = evt.Drone.Id,
                Model = evt.Drone.Model,
                HomeLocation = Location.FromDto(evt.Drone.HomeLocation),
                FirstSeenAt = evt.ReceivedAt,
                LastSeenAt = evt.ReceivedAt
            };
            await _collection.InsertOneAsync(doc);
        }
        else
        {
            var update = Builders<DroneRegistryDocument>.Update
                .Set(d => d.Model, evt.Drone.Model)
                .Set(d => d.HomeLocation, Location.FromDto(evt.Drone.HomeLocation))
                .Set(d => d.LastSeenAt, evt.ReceivedAt);

            await _collection.UpdateOneAsync(filter, update);
        }
    }

    private async Task HandleDroneDisconnected(DroneDisconnected evt)
    {
        var filter = Builders<DroneRegistryDocument>.Filter.Eq(d => d.DroneId, evt.DroneId);
        var existing = await _collection.Find(filter).FirstOrDefaultAsync();

        if (existing != null)
        {
            var update = Builders<DroneRegistryDocument>.Update
                .Set(d => d.LastSeenAt, evt.ReceivedAt);
            await _collection.UpdateOneAsync(filter, update);
        }
    }

    private async Task HandleTelemetryReceived(DroneTelemetryReceived evt)
    {
        var filter = Builders<DroneRegistryDocument>.Filter.Eq(d => d.DroneId, evt.Drone.Id);
        var existing = await _collection.Find(filter).FirstOrDefaultAsync();

        if (existing == null)
        {
            var doc = new DroneRegistryDocument
            {
                DroneId = evt.Drone.Id,
                Model = evt.Drone.Model,
                HomeLocation = Location.FromDto(evt.Drone.HomeLocation),
                FirstSeenAt = evt.ReceivedAt,
                LastSeenAt = evt.ReceivedAt
            };
            await _collection.InsertOneAsync(doc);
            return;
        }

        // Update only if static metadata changed
        bool changed = existing.Model != evt.Drone.Model
                       || !existing.HomeLocation.Equals(Location.FromDto(evt.Drone.HomeLocation));

        var updateBuilder = Builders<DroneRegistryDocument>.Update.Set(d => d.LastSeenAt, evt.ReceivedAt);

        if (changed)
        {
            updateBuilder = updateBuilder
                .Set(d => d.Model, evt.Drone.Model)
                .Set(d => d.HomeLocation, Location.FromDto(evt.Drone.HomeLocation));
        }

        await _collection.UpdateOneAsync(filter, updateBuilder);
    }
}
