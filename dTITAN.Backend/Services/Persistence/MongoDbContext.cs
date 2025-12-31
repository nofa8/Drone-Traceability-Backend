using MongoDB.Driver;

namespace dTITAN.Backend.Services.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _db;

    public MongoDbContext(IConfiguration config)
    {
        var connectionString = config.GetConnectionString("MongoDb");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("MongoDB connection string not configured.");

        var client = new MongoClient(connectionString);
        var databaseName = config["MongoDbDatabaseName"];
        if (string.IsNullOrWhiteSpace(databaseName))
            databaseName = "dTITAN";

        _db = client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) => _db.GetCollection<T>(name);
}

