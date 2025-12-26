using MongoDB.Driver;
using dTITAN.Backend.Data;
using dTITAN.Backend.Data.Documents;
using dTITAN.Backend.EventBus;
using dTITAN.Backend.Data.Events;

namespace dTITAN.Backend.Services.Persistence;

public class DroneRegistryWriter
{
    private readonly IMongoCollection<DroneRegistryDocument> _collection;

    public DroneRegistryWriter(MongoDbContext db, IDroneEventBus eventBus)
    {
        _collection = db.GetCollection<DroneRegistryDocument>("drone_registry");

        // Ensure DroneId is unique in MongoDB
        var keys = Builders<DroneRegistryDocument>.IndexKeys.Ascending(d => d.DroneId);
        _collection.Indexes.CreateOne(new CreateIndexModel<DroneRegistryDocument>(keys, new CreateIndexOptions { Unique = true }));

        // Subscribe to events
        eventBus.Subscribe<DroneConnected>(HandleDroneConnected);
        eventBus.Subscribe<DroneTelemetryReceived>(HandleTelemetryReceived);
        eventBus.Subscribe<DroneDisconnected>(HandleDroneDisconnected);
    }

    private Task HandleDroneConnected(DroneConnected evt)
        => UpsertDrone(evt.Drone.Id, evt.Drone.Model, evt.ReceivedAt, true);

    private Task HandleTelemetryReceived(DroneTelemetryReceived evt)
        => UpsertDrone(evt.Drone.Id, evt.Drone.Model, evt.ReceivedAt, true);

    private async Task HandleDroneDisconnected(DroneDisconnected evt)
    {
        var filter = Builders<DroneRegistryDocument>.Filter.Eq(d => d.DroneId, evt.DroneId);
        var update = Builders<DroneRegistryDocument>.Update
            .Set(d => d.IsConnected, false);

        await _collection.UpdateOneAsync(filter, update);
    }

    /// <summary>
    /// Upserts a drone document atomically using DroneId as key.
    /// FirstSeenAt is set only if document is new.
    /// LastSeenAt and IsConnected are updated every time.
    /// </summary>
    private async Task UpsertDrone(string droneId, string model, DateTime timestamp, bool isConnected)
    {
        var filter = Builders<DroneRegistryDocument>.Filter.Eq(d => d.DroneId, droneId);

        var update = Builders<DroneRegistryDocument>.Update
            .Set(d => d.Model, model)
            .SetOnInsert(d => d.FirstSeenAt, timestamp)
            .Set(d => d.LastSeenAt, timestamp)
            .Set(d => d.IsConnected, isConnected);

        await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }

}
