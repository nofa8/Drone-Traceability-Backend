using dTITAN.Backend.Data;
using dTITAN.Backend.Middleware;
using dTITAN.Backend.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// Serilog configuration
// ----------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // reads Serilog section from appsettings.json
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.With(new dTITAN.Backend.Logging.SourceContextEnricher())
    .WriteTo.Console(theme: dTITAN.Backend.Logging.AnsiConsoleThemes.Custom,
           outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{ThreadId:D3}] {Level:u3} {Message:lj} {ShortSourceContext}{NewLine}{Exception}")
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

// ----------------------
// Register services
// ----------------------
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<RedisService>();

builder.Services.AddSingleton<DroneMessageQueue>();

builder.Services.AddHostedService<WebSocketService>();
builder.Services.AddHostedService<DroneHistoryBackgroundWriter>();

builder.Services.AddScoped<IDroneService, DroneService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ----------------------
// Configure middleware
// ----------------------
app.UseWebSockets();
app.UseHttpsRedirection();
app.UseAuthorization();

// Rest API
app.MapControllers();

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
