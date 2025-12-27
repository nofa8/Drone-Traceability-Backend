
using dTITAN.Backend.Data.Documents;

namespace dTITAN.Backend.Data.Models;

public class TelemetryPage
{
    /// <summary>
    /// The list of telemetry documents returned for this page.
    /// </summary>
    public List<DroneTelemetryDocument> Items { get; set; } = [];

    /// <summary>
    /// Timestamp of the oldest document in this page. Use as cursor for previous page.
    /// </summary>
    public DateTime? PrevCursor { get; set; }

    /// <summary>
    /// Timestamp of the newest document in this page. Use as cursor for next page.
    /// </summary>
    public DateTime? NextCursor { get; set; }
}
