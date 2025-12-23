using dTITAN.Backend.Data;
using dTITAN.Backend.EventBus;
using dTITAN.Backend.Services;
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

// Singleton services
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<IDroneEventBus, InMemoryDroneEventBus>();

// Hosted services
builder.Services.AddHostedService<WebSocketService>();
builder.Services.AddHostedService<DroneHistoryBackgroundWriter>();

// Domain / optional services
builder.Services.AddScoped<IDroneService, DroneService>();

// Controllers / Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

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
