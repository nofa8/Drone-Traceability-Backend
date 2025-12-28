using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Mongo;
using dTITAN.Backend.Data.Mongo.Documents;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace dTITAN.Backend.Controllers;

/// <summary>
/// Provides access to drone snapshot data, including paginated snapshot history
/// and retrieval of the latest snapshot for a specific drone.
/// </summary>
[ApiController]
[Route("api/drones")]
public class DroneSnapshotController(
    IMongoCollection<DroneSnapshotDocument> snapshots,
    ILogger<DroneSnapshotController> logger) : ControllerBase
{
    private readonly IMongoCollection<DroneSnapshotDocument> _snapshots = snapshots;
    private readonly ILogger<DroneSnapshotController> _logger = logger;

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
    /// Default is 100; maximum allowed is 1000.
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
    /// A <see cref="PagedResult{DroneSnapshotDocument}"/> containing telemetry items and pagination cursors.
    /// Items are always returned sorted by <c>Timestamp</c> (latest first).
    /// </returns>
    [HttpGet("snapshots")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DroneSnapshotDocument>>> GetAll(
        [FromQuery] CursorPageRequest pageRequest,
        [FromQuery] bool? isConnected = null)
    {
        _logger.LogInformation("Fetching telemetry snapshots: isConnected={IsConnected}", isConnected);

        var filter = Builders<DroneSnapshotDocument>.Filter.Empty;
        if (isConnected.HasValue)
        {
            filter &= Builders<DroneSnapshotDocument>.Filter
                .Eq(d => d.IsConnected, isConnected.Value);
        }

        var page = await MongoCursorPagination.FetchPageAsync(
            _snapshots,
            filter,
            pageRequest,
            defaultLimit: 50,
            min: 1,
            max: 1000,
            _logger
        );
        return Ok(page);
    }

    /// <summary>
    /// Retrieves the most recent snapshot for a specific drone.
    /// </summary>
    /// <param name="droneId">
    /// The unique identifier of the drone.
    /// </param>
    /// <returns>
    /// The latest <see cref="DroneSnapshotDocument"/> for the given drone,
    /// or <c>404 Not Found</c> if no snapshot exists.
    /// </returns>
    [HttpGet("/{droneId}/snapshot")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DroneSnapshotDocument>> Get(string droneId)
    {
        var doc = await _snapshots
            .Find(d => d.DroneId == droneId)
            .FirstOrDefaultAsync();

        if (doc is null) return NotFound();

        return Ok(doc);
    }
}
