using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using dTITAN.Backend.Data.Models;
using dTITAN.Backend.Data.Mongo;
using dTITAN.Backend.Data.Mongo.Documents;

namespace dTITAN.Backend.Controllers;

/// <summary>
/// Provides access to historical telemetry data for individual drones,
/// with support for time filtering and cursor-based pagination.
/// </summary>
[ApiController]
[Route("api/drones/{droneId}/telemetry")]
public class DroneTelemetryController(
    IMongoCollection<DroneTelemetryDocument> telemetries,
    ILogger<DroneTelemetryController> logger) : ControllerBase
{
    private readonly IMongoCollection<DroneTelemetryDocument> _collection = telemetries;
    private readonly ILogger<DroneTelemetryController> _logger = logger;

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
    /// </param>
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
    /// A <see cref="PagedResult{DroneTelemetryDocument}"/> containing telemetry items and pagination cursors.
    /// Items are always returned sorted by <c>Timestamp</c> (latest first).
    /// </returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DroneTelemetryDocument>>> Get(
        string droneId,
        [FromQuery] CursorPageRequest pageRequest)
    {
        _logger.LogInformation("Fetching telemetry for DroneId={DroneId}", droneId);

        var filter = Builders<DroneTelemetryDocument>.Filter.Eq(d => d.DroneId, droneId);

        var page = await MongoCursorPagination.FetchPageAsync(
            _collection,
            filter,
            pageRequest,
            defaultLimit: 50,
            min: 1,
            max: 1000,
            _logger
        );
        return Ok(page);
    }
}
