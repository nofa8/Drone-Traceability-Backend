using System.Diagnostics;
using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Driver;
using dTITAN.Backend.Data;
using dTITAN.Backend.Models;
using dTITAN.Backend.DTO;

namespace dTITAN.Backend.Services;

/// <summary>
/// Background service that consumes drone messages from the shared
/// <see cref="DroneMessageQueue"/>, transforms them into MongoDB documents
/// and writes them to the configured collection in efficient batches.
/// </summary>
/// <param name="queue">Queue providing incoming drone messages.</param>
/// <param name="db">MongoDB context used to obtain the target collection.</param>
/// <param name="logger">Logger used for diagnostics and error reporting.</param>
public class DroneHistoryBackgroundWriter(DroneMessageQueue queue, MongoDbContext db, ILogger<DroneHistoryBackgroundWriter> logger) : BackgroundService
{
    private readonly DroneMessageQueue _queue = queue;
    private readonly IMongoCollection<BsonDocument> _col = db.GetCollection<BsonDocument>("drone_history");
    private readonly int _batchSize = 200;
    private readonly TimeSpan _maxWait = TimeSpan.FromSeconds(1);
    private readonly ILogger<DroneHistoryBackgroundWriter> _logger = logger;

    /// <summary>
    /// Background execution loop. Reads batches of messages from the queue,
    /// deserializes payloads into <see cref="dTITAN.Backend.Models.Drone"/>
    /// instances and inserts them into MongoDB. Retries transient failures.
    /// </summary>
    /// <param name="stoppingToken">Token that signals service shutdown.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var batch in ReadBatchesAsync(stoppingToken))
        {
            if (batch.Count == 0) continue;

            var docs = new List<BsonDocument>(batch.Count);
            foreach (var droneEnvelope in batch)
            {
                try
                {
                    var drone = System.Text.Json.JsonSerializer.Deserialize<Drone>(droneEnvelope.Payload);
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
                    _logger.LogError(ex, "Failed to deserialize drone message for background write");
                }
            }

            if (docs.Count == 0) continue;

            int attempts = 0;
            while (true)
            {
                try
                {
                    await _col.InsertManyAsync(docs, cancellationToken: stoppingToken);
                    break;
                }
                catch (Exception ex) when (attempts++ < 3)
                {
                    _logger.LogError(ex, "Insert batch failed (attempt {Attempt}). Retrying in 500ms.", attempts);
                    await Task.Delay(500, stoppingToken);
                }
            }
        }
    }

    /// <summary>
    /// Reads messages from the queue and groups them into batches optimized for
    /// bulk insertion. Batches are returned when either the configured batch size
    /// is reached or the configured maximum wait time elapses.
    /// </summary>
    /// <param name="ct">Cancellation token used to stop iteration.</param>
    /// <returns>An async-enumerable yielding lists of <see cref="DroneEnvelope"/>.</returns>
    private async IAsyncEnumerable<List<DroneEnvelope>> ReadBatchesAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var batch = new List<DroneEnvelope>(_batchSize);
        var enumerator = _queue.ReadAllAsync(ct).GetAsyncEnumerator(ct);
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

                yield return batch;
                batch = new List<DroneEnvelope>(_batchSize);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
}
