using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Persistence;
using dTITAN.Backend.Data.Transport.Rest;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace dTITAN.Backend.Services.Controllers;

/// <summary>
/// Provides access to drone snapshot data, including paginated snapshot history
/// and retrieval of the latest snapshot for a specific drone.
/// </summary>
[ApiController]
[Route("api/drones")]
public class DroneController(
    IMongoCollection<DroneSnapshotDocument> snapshots,
    ILogger<DroneController> logger) : ControllerBase
{
    private readonly IMongoCollection<DroneSnapshotDocument> _snapshots = snapshots;
    private readonly ILogger<DroneController> _logger = logger;

    /// <summary>
    /// Retrieves a paginated list of drone snapshots. Supports optional filtering
    /// by connection state, time range, and cursor-based pagination.
    /// </summary>
    /// <param name="isConnected">
    /// Optional. If specified, only snapshots matching the given connection state
    /// will be returned.
    /// </param>
    /// <param name="from">
    /// Optional. Start of the time range (inclusive). Only snapshots with
    /// <c>Timestamp &gt;= from</c> will be returned.
    /// </param>
    /// <param name="to">
    /// Optional. End of the time range (inclusive). Only snapshots with
    /// <c>Timestamp &lt;= to</c> will be returned.
    /// </param>
    /// <param name="limit">
    /// Optional. Maximum number of snapshots to return in a single page.
    /// Default is 50; maximum allowed is 1000.
    /// </param>
    /// <param name="cursor">
    /// Optional. Timestamp of the last snapshot from a previous page.
    /// Used as a pagination cursor.
    /// </param>
    /// <param name="forward">
    /// Optional. Pagination direction relative to the cursor:
    /// <list type="bullet">
    /// <item><description>false (default): fetch older snapshots (&lt; cursor)</description></item>
    /// <item><description>true: fetch newer snapshots (&gt; cursor)</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// A <see cref="PagedResult{Drone}"/> containing telemetry items and pagination cursors.
    /// Items are always returned sorted by <c>Timestamp</c> (latest first).
    /// </returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DroneSnapshot>>> GetAll(
        [FromQuery] CursorPageRequest pageRequest,
        [FromQuery] bool? isConnected = null)
    {
        _logger.LogInformation("Fetching telemetry snapshots: isConnected={IsConnected}", isConnected);

        const int defaultLimit = 50;
        const int min = 1;
        const int max = 1000;

        int limit;
        if (pageRequest.Limit > max)
        {
            _logger.LogWarning("Requested limit {Limit} exceeds max; capping to {Max}", pageRequest.Limit, max);
            limit = max;
        }
        else if (pageRequest.Limit < min)
        {
            _logger.LogWarning("Requested limit {Limit} is invalid; resetting to {defaultLimit}", pageRequest.Limit, defaultLimit);
            limit = defaultLimit;
        }
        else
        {
            limit = pageRequest.Limit;
        }

        var f = Builders<DroneSnapshotDocument>.Filter;
        var filter = f.Empty;
        if (isConnected.HasValue)
        {
            filter &= f.Eq(d => d.IsConnected, isConnected.Value);
        }
        if (pageRequest.From.HasValue)
        {
            filter &= f.Gte(d => d.Telemetry.Timestamp, pageRequest.From.Value);
        }
        if (pageRequest.To.HasValue)
        {
            filter &= f.Lte(d => d.Telemetry.Timestamp, pageRequest.To.Value);
        }
        if (pageRequest.Cursor.HasValue)
        {
            filter &= pageRequest.Forward
                ? f.Gt(d => d.Telemetry.Timestamp, pageRequest.Cursor.Value)
                : f.Lt(d => d.Telemetry.Timestamp, pageRequest.Cursor.Value);
        }

        var s = Builders<DroneSnapshotDocument>.Sort;
        var sort = pageRequest.Forward
            ? s.Ascending(d => d.Telemetry.Timestamp)
            : s.Descending(d => d.Telemetry.Timestamp);

        var docs = await _snapshots
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .Project<DroneSnapshot>(Builders<DroneSnapshotDocument>.Projection
                .Exclude(d => d.Id))
            .ToListAsync();

        if (pageRequest.Forward) docs.Reverse();

        var page = new PagedResult<DroneSnapshot>
        {
            Items = docs,
            PrevCursor = docs.LastOrDefault()?.Telemetry.Timestamp,
            NextCursor = docs.FirstOrDefault()?.Telemetry.Timestamp
        };
        return Ok(page);
    }

    /// <summary>
    /// Retrieves the most recent snapshot for a specific drone.
    /// </summary>
    /// <param name="droneId">
    /// The unique identifier of the drone.
    /// </param>
    /// <returns>
    /// The latest <see cref="DroneSnapshot"/> for the given drone,
    /// or <c>404 Not Found</c> if no snapshot exists.
    /// </returns>
    [HttpGet("{droneId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DroneSnapshot>> Get(string droneId)
    {
        _logger.LogInformation("Fetching telemetry snapshot for DroneId={DroneId}", droneId);
        var doc = await _snapshots
            .Find(d => d.DroneId == droneId)
            .Project<DroneSnapshot>(Builders<DroneSnapshotDocument>.Projection
                .Exclude(d => d.Id))
            .FirstOrDefaultAsync();

        if (doc is null) return NotFound();

        return Ok(doc);
    }
}
