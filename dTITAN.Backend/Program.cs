using dTITAN.Backend.Data;
using dTITAN.Backend.Middleware;
using dTITAN.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<RedisService>();
builder.Services.AddHostedService<ExternalWebSocketService>();
builder.Services.AddScoped<IDroneService, DroneService>();

// Drone history queue and background writer
builder.Services.AddSingleton<DroneMessageQueue>();
builder.Services.AddSingleton<QueueingSubscriber>();
builder.Services.AddHostedService<DroneHistoryBackgroundWriter>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseWebSockets();
app.UseHttpsRedirection();
app.UseAuthorization();

// Rest API
app.MapControllers();

// WebSocket Middleware 
app.UseMiddleware<ClientWebSocketMiddleware>();

app.Run();