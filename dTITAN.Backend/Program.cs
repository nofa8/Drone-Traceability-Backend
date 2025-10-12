using dTITAN.Backend.Data;
using dTITAN.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Register MongoDbContext and RedisService
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<RedisService>();

// Register DroneService with its interface
builder.Services.AddScoped<IDroneService, DroneService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

var app = builder.Build();

// app.UseSwagger();
// app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
