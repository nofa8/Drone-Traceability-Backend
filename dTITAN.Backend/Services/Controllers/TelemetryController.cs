using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using dTITAN.Backend.Data.Persistence;
using dTITAN.Backend.Data.Transport.Rest;
using dTITAN.Backend.Data.Models;

namespace dTITAN.Backend.Services.Controllers;

/// <summary>
/// Provides access to historical telemetry data for individual drones,
/// with support for time filtering and cursor-based pagination.
/// </summary>
[ApiController]
[Route("api/drones/{droneId}/telemetry")]
public class TelemetryController(
    IMongoCollection<DroneTelemetryDocument> telemetries,
    ILogger<TelemetryController> logger) : ControllerBase
{
    private readonly IMongoCollection<DroneTelemetryDocument> _telemetries = telemetries;
    private readonly ILogger<TelemetryController> _logger = logger;

    /// <summary>
    /// Retrieves a page of telemetry history for a specific drone.
    /// </summary>
    /// <param name="droneId">
    /// The unique identifier of the drone whose telemetry is being queried.
    /// </param>
    /// <param name="from">
    /// Optional. Start of the time range (inclusive).
    /// Only telemetry with <c>Timestamp &gt;= from</c> is returned.
    /// </param>
    /// <param name="to">
    /// Optional. End of the time range (inclusive).
    /// Only telemetry with <c>Timestamp &lt;= to</c> is returned.
    /// </param>
    /// <param name="limit">
    /// Optional. Maximum number of telemetry entries to return.
    /// Default is 100; maximum allowed is 1000.
    /// </param>i
    /// <param name="cursor">
    /// Optional. Timestamp used as a pagination cursor.
    /// Represents the last item from a previous page.
    /// </param>
    /// <param name="forward">
    /// Optional. Pagination direction relative to the cursor:
    /// <list type="bullet">
    /// <item><description>false (default): fetch older telemetry (&lt; cursor)</description></item>
    /// <item><description>true: fetch newer telemetry (&gt; cursor)</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// A <see cref="PagedResult{Telemetry}"/> containing telemetry items and pagination cursors.
    /// Items are always returned sorted by <c>Timestamp</c> (latest first).
    /// </returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Telemetry>>> Get(
        string droneId,
        [FromQuery] CursorPageRequest pageRequest)
    {
        _logger.LogInformation("Fetching telemetry for DroneId={DroneId}", droneId);
        const int defaultLimit = 50;
        const int min = 1;
        const int max = 1000;

        int limit;
        if (pageRequest.Limit > max)
        {
            _logger.LogWarning(
                "Requested telemetry page size {RequestedLimit} exceeds maximum {Max}. Capping to {Max}.",
                pageRequest.Limit,
                max,
                max);
            limit = max;
        }
        else if (pageRequest.Limit < min)
        {
            _logger.LogWarning(
                "Requested telemetry page size {RequestedLimit} is below minimum {Min}. Using default limit {DefaultLimit}.",
                pageRequest.Limit,
                min,
                defaultLimit);
            limit = defaultLimit;
        }
        else
        {
            limit = pageRequest.Limit;
        }

        var f = Builders<DroneTelemetryDocument>.Filter;
        var filter = f.Eq(d => d.DroneId, droneId);
        if (pageRequest.From.HasValue)
        {
            filter &= f.Gte(d => d.Timestamp, pageRequest.From.Value);
        }
        if (pageRequest.To.HasValue)
        {
            filter &= f.Lte(d => d.Timestamp, pageRequest.To.Value);
        }
        if (pageRequest.Cursor.HasValue)
        {
            filter &= pageRequest.Forward
                ? f.Gt(d => d.Timestamp, pageRequest.Cursor.Value)
                : f.Lt(d => d.Timestamp, pageRequest.Cursor.Value);
        }

        var s = Builders<DroneTelemetryDocument>.Sort;
        var sort = pageRequest.Forward
            ? s.Ascending(d => d.Timestamp)
            : s.Descending(d => d.Timestamp);

        var docs = await _telemetries
           .Find(filter)
           .Sort(sort)
           .Limit(limit)
           .Project<Telemetry>(Builders<DroneTelemetryDocument>.Projection
               .Exclude(d => d.Id)
               .Exclude(d => d.DroneId))
           .ToListAsync();


        if (pageRequest.Forward) docs.Reverse();

        var page = new PagedResult<Telemetry>
        {
            Items = docs,
            PrevCursor = docs.LastOrDefault()?.Timestamp,
            NextCursor = docs.FirstOrDefault()?.Timestamp
        };
        return Ok(page);
    }
}
