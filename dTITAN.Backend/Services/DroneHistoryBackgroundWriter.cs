using System.Diagnostics;
using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Channels;
using dTITAN.Backend.Data;
using dTITAN.Backend.Models;
using dTITAN.Backend.DTO;
using dTITAN.Backend.EventBus;
using dTITAN.Backend.Events;

namespace dTITAN.Backend.Services;

public class DroneHistoryBackgroundWriter(IDroneEventBus eventBus, MongoDbContext db, ILogger<DroneHistoryBackgroundWriter> logger) : BackgroundService
{
    private readonly IDroneEventBus _eventBus = eventBus;
    private readonly IMongoCollection<BsonDocument> _col = db.GetCollection<BsonDocument>("drone_history");
    private readonly int _batchSize = 200;
    private readonly TimeSpan _maxWait = TimeSpan.FromSeconds(1);
    private readonly ILogger<DroneHistoryBackgroundWriter> _logger = logger;
    private readonly Channel<Drone> _channel = Channel.CreateUnbounded<Drone>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DroneHistoryBackgroundWriter started");

        // subscribe to telemetry events and push Drone instances into the channel
        _eventBus.Subscribe<DroneTelemetryReceived>(evt =>
        {
            var written = _channel.Writer.TryWrite(evt.Drone);
            if (written)
                _logger.LogDebug("Enqueued telemetry for drone {DroneId}", evt.Drone?.Id);
            else
                _logger.LogWarning("Failed to enqueue telemetry for drone {DroneId}", evt.Drone?.Id);

            return Task.CompletedTask;
        });

        await foreach (var batch in ReadBatchesAsync(stoppingToken))
        {
            if (batch.Count == 0) continue;

            var docs = new List<BsonDocument>(batch.Count);
            foreach (var drone in batch)
            {
                try
                {
                    var doc = new BsonDocument
                    {
                        ["droneId"] = drone?.Id ?? ObjectId.GenerateNewId().ToString(),
                        ["lat"] = drone?.Latitude ?? 0,
                        ["lng"] = drone?.Longitude ?? 0,
                        ["alt"] = drone?.Altitude ?? 0,
                        ["batLvl"] = drone?.BatteryLevel ?? 0,
                        ["model"] = drone?.Model ?? string.Empty,
                        ["receivedAt"] = DateTime.UtcNow
                    };
                    docs.Add(doc);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to convert drone for background write");
                }
            }

            if (docs.Count == 0) continue;

            int attempts = 0;
            while (true)
            {
                try
                {
                    await _col.InsertManyAsync(docs, cancellationToken: stoppingToken);
                    _logger.LogInformation("Inserted {Count} drone history documents", docs.Count);
                    break;
                }
                catch (Exception ex) when (attempts++ < 3)
                {
                    _logger.LogError(ex, "Insert batch failed (attempt {Attempt}). Retrying in 500ms.", attempts);
                    await Task.Delay(500, stoppingToken);
                }
            }
        }

        _logger.LogInformation("DroneHistoryBackgroundWriter stopping");
    }

    /// <summary>
    /// Reads messages from the queue and groups them into batches optimized for
    /// bulk insertion. Batches are returned when either the configured batch size
    /// is reached or the configured maximum wait time elapses.
    /// </summary>
    /// <param name="ct">Cancellation token used to stop iteration.</param>
    /// <returns>An async-enumerable yielding lists of <see cref="DroneEnvelope"/>.</returns>
    private async IAsyncEnumerable<List<Drone>> ReadBatchesAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var batch = new List<Drone>(_batchSize);
        var enumerator = _channel.Reader.ReadAllAsync(ct).GetAsyncEnumerator(ct);
        try
        {
            while (await enumerator.MoveNextAsync())
            {
                batch.Add(enumerator.Current);

                var sw = Stopwatch.StartNew();
                while (batch.Count < _batchSize && sw.Elapsed < _maxWait)
                {
                    if (await enumerator.MoveNextAsync())
                    {
                        batch.Add(enumerator.Current);
                    }
                    else
                    {
                        break;
                    }
                }

                _logger.LogDebug("Yielding batch of {Count} telemetry items for write", batch.Count);
                yield return batch;
                batch = new List<Drone>(_batchSize);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
}
