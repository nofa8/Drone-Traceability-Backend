namespace dTITAN.Backend.Data.Models;

public sealed class TelemetryPersistenceState
{
    public Telemetry LastPersisted { get; init; } = default!;
    public DateTime LastPersistedAt { get; init; }
}
