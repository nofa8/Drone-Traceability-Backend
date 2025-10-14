using System.Diagnostics;
using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Driver;
using dTITAN.Backend.Data;
using dTITAN.Backend.Models;

namespace dTITAN.Backend.Services;

public class DroneHistoryBackgroundWriter : BackgroundService
{
    private readonly DroneMessageQueue _queue;
    private readonly IMongoCollection<BsonDocument> _col;
    private readonly int _batchSize;
    private readonly TimeSpan _maxWait;

    public DroneHistoryBackgroundWriter(DroneMessageQueue queue, MongoDbContext db)
    {
        _queue = queue;
        _col = db.GetCollection<BsonDocument>("drone_history");
        _batchSize = 200;
        _maxWait = TimeSpan.FromSeconds(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var batch in ReadBatchesAsync(stoppingToken))
        {
            if (batch.Count == 0) continue;

            var docs = new List<BsonDocument>(batch.Count);
            foreach (var json in batch)
            {
                try
                {
                    var drone = System.Text.Json.JsonSerializer.Deserialize<Drone>(json);
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
                    Console.Error.WriteLine($"Failed to deserialize drone message for background write: {ex}");
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
                    Console.Error.WriteLine($"Insert batch failed (attempt {attempts}): {ex}. Retrying in 500ms.");
                    await Task.Delay(500, stoppingToken);
                }
            }
        }
    }

    private async IAsyncEnumerable<List<string>> ReadBatchesAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var batch = new List<string>(_batchSize);
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
                batch = new List<string>(_batchSize);
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }
}
