using MongoDB.Driver;
using Serilog;
using System.Threading.Channels;
using dTITAN.Backend.Data.Persistence;
using dTITAN.Backend.Services.ClientGateway;
using dTITAN.Backend.Services.DroneGateway;
using dTITAN.Backend.Services.EventBus;
using dTITAN.Backend.Services.Persistence;

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
    return db.GetCollection<DroneTelemetryDocument>("drone_telemetry");
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
    return db.GetCollection<DroneCommandDocument>("drone_command");
});

// Event bus
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// Persistence services
builder.Services.AddSingleton<DroneTelemetryWriter>();
builder.Services.AddSingleton<DroneSnapshotUpdater>();
builder.Services.AddSingleton<CommandWriter>();

// Ingestion services
var disconnectTimeout = TimeSpan.FromSeconds(5);
builder.Services.AddSingleton(sp =>
    new DroneManager(
        sp.GetRequiredService<IEventBus>(),
        disconnectTimeout,
        sp.GetRequiredService<ILogger<DroneManager>>())
);

// Client Gateway services
builder.Services.AddSingleton(Channel.CreateUnbounded<(Guid, string)>());
builder.Services.AddSingleton<ClientConnectionManager>();
builder.Services.AddSingleton<ClientWebSocketService>();

// Hosted services
builder.Services.AddHostedService<ClientMessageProcessor>();
builder.Services.AddHostedService<DroneWebSocketClient>();
builder.Services.AddHostedService(sp =>
    new DroneTimeoutWorker(
        sp.GetRequiredService<DroneManager>(),
        disconnectTimeout)
);

// Controllers and Documentation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP API (controllers) - HTTP/2 + HTTP/1.1
    options.ListenAnyIP(5101, listen =>
    {
        listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });

    // WebSocket endpoint - HTTP/1.1 only
    options.ListenAnyIP(5102, listen =>
    {
        listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });
});

var app = builder.Build();

// Map WebSocket endpoint
app.MapWhen(
    context => context.Connection.LocalPort == 5102,
    wsApp =>
{
    wsApp.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

    wsApp.Run(async context =>
    {
        var lifetime = context.RequestServices.GetRequiredService<IHostApplicationLifetime>();

        var wsService = context.RequestServices.GetRequiredService<ClientWebSocketService>();
        await wsService.HandleClientAsync(context, lifetime.ApplicationStopping);
    });
});

app.MapOpenApi();
// XXX: HTTPS redirection requires proper certs.
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure services are constructed so they subscribe to events
app.Services.GetRequiredService<ClientConnectionManager>();
app.Services.GetRequiredService<DroneTelemetryWriter>();
app.Services.GetRequiredService<DroneSnapshotUpdater>();
app.Services.GetRequiredService<CommandWriter>();

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
