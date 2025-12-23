using System.Text.Json;
using dTITAN.Backend.EventBus;
using Microsoft.Extensions.Logging;

namespace dTITAN.Backend.Services;

public class WebSocketService : BackgroundService
{
    private readonly DroneConnectionManager _manager;
    private readonly ILogger<WebSocketService> _logger;
    private readonly string? _connectionString;

    public WebSocketService(
        IConfiguration config,
        IDroneEventBus eventBus,
        ILoggerFactory loggerFactory,
        ILogger<WebSocketService> logger)
    {
        _logger = logger;

        _connectionString = config.GetConnectionString("DroneWS");
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Drone WebSocket not configured.");

        _logger.LogInformation("Initializing WebSocketService with {Uri}", _connectionString);
        _manager = new DroneConnectionManager(
            new Uri(_connectionString),
            eventBus,
            loggerFactory
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var droneIds = await LoadDroneIdsAsync(stoppingToken);
        _logger.LogInformation("Starting drone connection manager for {Count} drones", droneIds.Count());
        try
        {
            await _manager.StartAsync(droneIds, stoppingToken);
            _logger.LogInformation("Drone connection manager tasks completed");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("WebSocketService cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocketService execution failed");
            throw;
        }
    }

    private async Task<IEnumerable<string>> LoadDroneIdsAsync(
        CancellationToken ct)
    {
        try
        {
            var builder = new UriBuilder(_connectionString!);
            if (builder.Scheme == "ws") builder.Scheme = "http";
            else if (builder.Scheme == "wss") builder.Scheme = "https";
            builder.Path = "/drones";

            var httpUri = builder.Uri;
            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            using var res = await http.GetAsync(httpUri, ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to load drone ids from {Uri}: {StatusCode}", httpUri, res.StatusCode);
                // XXX: defaulting to testing ids
                return ["1", "2", "3"];
            }

            var json = await res.Content.ReadAsStringAsync(ct);
            var ids = JsonSerializer.Deserialize<IEnumerable<string>>(json) ?? [];
            return ids;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading drone ids");
            return [];
        }
    }
}
