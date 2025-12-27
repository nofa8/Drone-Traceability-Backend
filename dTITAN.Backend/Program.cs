using DnsClient.Protocol;
using dTITAN.Backend.Data;
using dTITAN.Backend.Data.Documents;
using dTITAN.Backend.EventBus;
using dTITAN.Backend.Services.Domain;
using dTITAN.Backend.Services.Ingestion;
using dTITAN.Backend.Services.Persistence;
using MongoDB.Driver;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.With(new dTITAN.Backend.Logging.SourceContextEnricher())
    .WriteTo.Console(theme: dTITAN.Backend.Logging.AnsiConsoleThemes.Custom,
           outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{ThreadId:D3}] {Level:u3} {Message:lj} {ShortSourceContext}{NewLine}{Exception}")
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

// Singletons
// MongoDB collections
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton(sp =>
{
    var db = sp.GetRequiredService<MongoDbContext>();
    var collection = db.GetCollection<DroneRegistryDocument>("drone_registry");

    // Ensure DroneId is unique in MongoDB
    var keys = Builders<DroneRegistryDocument>.IndexKeys.Ascending(d => d.DroneId);
    // XXX: Index can make start slow with large datasets
    collection.Indexes.CreateOne(
        new CreateIndexModel<DroneRegistryDocument>(keys, new CreateIndexOptions { Unique = true })
    );
    return collection;
});
builder.Services.AddSingleton(sp =>
{
    var db = sp.GetRequiredService<MongoDbContext>();
    var collection = db.GetCollection<DroneSnapshotDocument>("drone_snapshot");

    // Ensure DroneId is unique in MongoDB
    var keys = Builders<DroneSnapshotDocument>.IndexKeys.Ascending(d => d.DroneId);
    // XXX: Index can make start slow with large datasets
    collection.Indexes.CreateOne(
        new CreateIndexModel<DroneSnapshotDocument>(keys, new CreateIndexOptions { Unique = true })
    );
    return collection;
});
builder.Services.AddSingleton(sp =>
{
    var db = sp.GetRequiredService<MongoDbContext>();
    return db.GetCollection<DroneTelemetryDocument>("drone_telemetry");
});

// Event bus
builder.Services.AddSingleton<IDroneEventBus, InMemoryDroneEventBus>();

// Persistence services
builder.Services.AddSingleton<DroneTelemetryWriter>();
builder.Services.AddSingleton<DroneRegistryWriter>();
builder.Services.AddSingleton<DroneSnapshotWriter>();

// Ingestion services
var disconnectTimeout = TimeSpan.FromSeconds(5);
builder.Services.AddSingleton(sp =>
    new DroneManager(
        sp.GetRequiredService<IDroneEventBus>(),
        disconnectTimeout,
        sp.GetRequiredService<ILogger<DroneManager>>())
);

// Hosted services
builder.Services.AddHostedService<DroneWebSocketClient>();
builder.Services.AddHostedService(sp =>
    new DroneTimeoutWorker(
        sp.GetRequiredService<DroneManager>(),
        disconnectTimeout)
);

// Domain / optional services
builder.Services.AddScoped<IDroneService, DroneService>();

// Controllers / Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Ensure persistence writers are constructed so they subscribe to events
app.Services.GetRequiredService<DroneTelemetryWriter>();
app.Services.GetRequiredService<DroneRegistryWriter>();
app.Services.GetRequiredService<DroneSnapshotWriter>();

// Middleware
app.UseHttpsRedirection();
app.UseAuthorization();

// REST API
app.MapControllers();

// Run
try
{
    Log.Information("Starting dTITAN Backend");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
