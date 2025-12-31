using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using dTITAN.Backend.Data.Persistence;
using dTITAN.Backend.Data.Transport.Rest;
using dTITAN.Backend.Data.Models;

namespace dTITAN.Backend.Services.Controllers;

/// <summary>
/// Provides access to historical command data for individual drones,
/// with support for time filtering and cursor-based pagination.
/// </summary>
[ApiController]
[Route("api/drones/{droneId}/commands")]
public class CommandsController(
    IMongoCollection<DroneCommandDocument> commands,
    ILogger<CommandsController> logger) : ControllerBase
{
    private readonly IMongoCollection<DroneCommandDocument> _commands = commands;
    private readonly ILogger<CommandsController> _logger = logger;

    /// <summary>
    /// Retrieves a page of command history for a specific drone.
    /// </summary>
    /// <param name="droneId">
    /// The unique identifier of the drone whose commands are being queried.
    /// </param>
    /// <param name="commandType">
    /// Optional. If specified, only commands with the given command type
    /// (for example, "FlightCommand", "UtilityCommand", "StartMissionCommand")
    /// are returned. Matching is exact on the command type name.
    /// </param>
    /// <param name="from">
    /// Optional. Start of the time range (inclusive).
    /// Only commands with <c>Timestamp &gt;= from</c> are returned.
    /// </param>
    /// <param name="to">
    /// Optional. End of the time range (inclusive).
    /// Only commands with <c>Timestamp &lt;= to</c> are returned.
    /// </param>
    /// <param name="limit">
    /// Optional. Maximum number of command entries to return.
    /// Default is 100; maximum allowed is 1000.
    /// </param>
    /// <param name="cursor">
    /// Optional. Timestamp used as a pagination cursor.
    /// Represents the last item from a previous page.
    /// </param>
    /// <param name="forward">
    /// Optional. Pagination direction relative to the cursor:
    /// <list type="bullet">
    /// <item><description>false (default): fetch older commands (&lt; cursor)</description></item>
    /// <item><description>true: fetch newer commands (&gt; cursor)</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// A <see cref="PagedResult{DroneCommandContext}"/> containing command items and pagination cursors.
    /// Items are always returned sorted by <c>Timestamp</c> (latest first).
    /// </returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DroneCommandContext>>> Get(
        string droneId,
        [FromQuery] string? commandType,
        [FromQuery] CursorPageRequest pageRequest)
    {
        _logger.LogInformation("Fetching commands for DroneId={DroneId}", droneId);

        const int defaultLimit = 50;
        const int min = 1;
        const int max = 1000;

        int limit;
        if (pageRequest.Limit > max)
        {
            _logger.LogWarning(
                "Requested limit {RequestedLimit} exceeds maximum {MaxLimit}. Capping to {AppliedLimit}.",
                pageRequest.Limit,
                max,
                max);
            limit = max;
        }
        else if (pageRequest.Limit < min)
        {
            _logger.LogWarning(
                "Requested limit {RequestedLimit} is below minimum {MinLimit}. Using default limit {DefaultLimit}.",
                pageRequest.Limit,
                min,
                defaultLimit);
            limit = defaultLimit;
        }
        else
        {
            limit = pageRequest.Limit;
        }

        var f = Builders<DroneCommandDocument>.Filter;
        var filter = f.Eq(d => d.DroneId, droneId);
        if (!string.IsNullOrEmpty(commandType))
        {
            filter &= f.Eq(d => d.CommandType, commandType);
        }
        if (pageRequest.From.HasValue)
        {
            filter &= f.Gte(d => d.TimeStamp, pageRequest.From.Value);
        }
        if (pageRequest.To.HasValue)
        {
            filter &= f.Lte(d => d.TimeStamp, pageRequest.To.Value);
        }
        if (pageRequest.Cursor.HasValue)
        {
            filter &= pageRequest.Forward
                ? f.Gt(d => d.TimeStamp, pageRequest.Cursor.Value)
                : f.Lt(d => d.TimeStamp, pageRequest.Cursor.Value);
        }

        var s = Builders<DroneCommandDocument>.Sort;
        var sort = pageRequest.Forward
            ? s.Ascending(d => d.TimeStamp)
            : s.Descending(d => d.TimeStamp);

        var docs = await _commands
           .Find(filter)
           .Sort(sort)
           .Limit(limit)
           .Project<DroneCommandContext>(Builders<DroneCommandDocument>.Projection
               .Exclude(d => d.Id))
           .ToListAsync();

        if (pageRequest.Forward) docs.Reverse();

        var page = new PagedResult<DroneCommandContext>
        {
            Items = docs,
            PrevCursor = docs.LastOrDefault()?.TimeStamp,
            NextCursor = docs.FirstOrDefault()?.TimeStamp
        };
        return Ok(page);
    }
}
