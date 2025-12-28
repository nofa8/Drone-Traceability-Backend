using MongoDB.Driver;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Mongo.Documents;

namespace dTITAN.Backend.Data.Mongo;

public static class MongoCursorPagination
{
    public static async Task<PagedResult<T>> FetchPageAsync<T>(
        IMongoCollection<T> collection,
        FilterDefinition<T> baseFilter,
        CursorPageRequest pageRequest,
        int defaultLimit,
        int min,
        int max,
        ILogger logger) where T : DroneTelemetryDocument
    {
        int limit;
        if (pageRequest.Limit > max)
        {
            logger.LogWarning("Requested limit {Limit} exceeds max; capping to {Max}", pageRequest.Limit, max);
            limit = max;
        }
        else if (pageRequest.Limit < min)
        {
            logger.LogWarning("Requested limit {Limit} is invalid; resetting to 100", pageRequest.Limit);
            limit = defaultLimit;
        }
        else
        {
            limit = pageRequest.Limit;
        }

        var filter = baseFilter;
        if (pageRequest.From.HasValue)
        {
            filter &= Builders<T>.Filter.Gte(d => d.Timestamp, pageRequest.From.Value);
        }
        if (pageRequest.To.HasValue)
        {
            filter &= Builders<T>.Filter.Lte(d => d.Timestamp, pageRequest.To.Value);
        }
        if (pageRequest.Cursor.HasValue)
        {
            filter &= pageRequest.Forward
                ? Builders<T>.Filter.Gt(d => d.Timestamp, pageRequest.Cursor.Value)
                : Builders<T>.Filter.Lt(d => d.Timestamp, pageRequest.Cursor.Value);
        }

        var sort = pageRequest.Forward
            ? Builders<T>.Sort
                .Ascending(d => d.Timestamp)
            : Builders<T>.Sort
                .Descending(d => d.Timestamp);

        var docs = await collection
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync();

        if (pageRequest.Forward) docs.Reverse();

        return new PagedResult<T>
        {
            Items = docs,
            PrevCursor = docs.LastOrDefault()?.Timestamp,
            NextCursor = docs.FirstOrDefault()?.Timestamp
        };
    }
}
