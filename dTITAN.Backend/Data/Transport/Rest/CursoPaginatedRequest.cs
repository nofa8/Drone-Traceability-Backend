namespace dTITAN.Backend.Data.Transport.Rest;

public sealed class CursorPageRequest
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public DateTime? Cursor { get; init; }
    public bool Forward { get; init; }
    public int Limit { get; init; }
}
