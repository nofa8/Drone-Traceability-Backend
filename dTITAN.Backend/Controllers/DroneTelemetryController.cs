using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using dTITAN.Backend.Data.Documents;
using dTITAN.Backend.Data.Models;
using System.Linq;

namespace dTITAN.Backend.Controllers;

[ApiController]
[Route("api/drones/{droneId}/telemetry")]
public class DroneTelemetryController(IMongoCollection<DroneTelemetryDocument> collection, ILogger<DroneTelemetryController> logger) : ControllerBase
{
    private readonly IMongoCollection<DroneTelemetryDocument> _collection = collection;
    private readonly ILogger<DroneTelemetryController> _logger = logger;

    /// <summary>
    /// Retrieves a page of telemetry history for a specific drone. Supports optional time range 
    /// filtering and cursor-based pagination.
    /// </summary>
    /// <param name="droneId">
    /// The unique identifier of the drone for which telemetry should be retrieved.
    /// </param>
    /// <param name="from">
    /// Optional. Start of the time range (inclusive). Only telemetry with 
    /// <c>Timestamp &gt;= from</c> will be returned.
    /// </param>
    /// <param name="to">
    /// Optional. End of the time range (inclusive). Only telemetry with 
    /// <c>Timestamp &lt;= to</c> will be returned.
    /// </param>
    /// <param name="limit">
    /// Optional. Maximum number of telemetry documents to return in a single page. Default is 100; 
    /// maximum allowed is 1000 to prevent overly large queries.
    /// </param>
    /// <param name="cursor">
    /// Optional. Timestamp of the last document from a previous page. Used as a pagination cursor. 
    /// If not provided, the most recent telemetry is returned.
    /// </param>
    /// <param name="forward">
    /// Optional. Direction of pagination relative to the cursor:
    /// <list type="bullet">
    /// <item><description>false (default): fetch older documents (&lt; cursor)</description></item>
    /// <item><description>true: fetch newer documents (&gt; cursor)</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// A <see cref="TelemetryPage"/> object containing:
    /// <list type="bullet">
    /// <item><description>
    /// <see cref="TelemetryPage.Items"/>: the list of telemetry documents for this page, sorted 
    /// descending by <c>Timestamp</c> (latest first).
    /// </description></item>
    /// <item><description>
    /// <see cref="TelemetryPage.NextCursor"/>: timestamp of the newest document in this page; use 
    /// to fetch the next page of newer telemetry.
    /// </description></item>
    /// <item><description>
    /// <see cref="TelemetryPage.PrevCursor"/>: timestamp of the oldest document in this page; use 
    /// to fetch the previous page of older telemetry.
    /// </description></item>
    /// </list>
    /// </returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TelemetryPage>> Get(
        string droneId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 100,
        [FromQuery] DateTime? cursor = null,
        [FromQuery] bool forward = false)
    {
        _logger.LogInformation("Fetching telemetry for DroneId={DroneId}", droneId);
        if (limit <= 0)
        {
            _logger.LogWarning("Requested limit {Limit} is invalid; resetting to default 100", limit);
            limit = 100;
        }
        else if (limit > 1000)
        {
            _logger.LogWarning("Requested limit {Limit} exceeds max; capping to 1000", limit);
            limit = 1000;
        }

        var filter = Builders<DroneTelemetryDocument>.Filter.Eq(d => d.DroneId, droneId);
        if (from.HasValue)
        {
            filter &= Builders<DroneTelemetryDocument>.Filter.Gte(d => d.Timestamp, from.Value);
        }
        if (to.HasValue)
        {
            filter &= Builders<DroneTelemetryDocument>.Filter.Lte(d => d.Timestamp, to.Value);
        }
        if (cursor.HasValue)
        {
            filter &= forward
                ? Builders<DroneTelemetryDocument>.Filter.Gt(d => d.Timestamp, cursor.Value)
                : Builders<DroneTelemetryDocument>.Filter.Lt(d => d.Timestamp, cursor.Value);
        }

        var sort = forward
            ? Builders<DroneTelemetryDocument>.Sort.Ascending(d => d.Timestamp)
            : Builders<DroneTelemetryDocument>.Sort.Descending(d => d.Timestamp);

        List<DroneTelemetryDocument> docs;
        try
        {
            docs = await _collection
                .Find(filter)
                .Sort(sort)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching telemetry for DroneId={DroneId}", droneId);
            throw;
        }

        // Ensure latest-first order in response
        if (forward) docs.Reverse();

        var page = new TelemetryPage
        {
            Items = docs,
            PrevCursor = docs.LastOrDefault()?.Timestamp,
            NextCursor = docs.FirstOrDefault()?.Timestamp
        };
        return Ok(page);
    }
}