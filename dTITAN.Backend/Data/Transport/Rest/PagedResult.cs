namespace dTITAN.Backend.Data.Transport.Rest;

public class PagedResult<T>
{
    /// <summary>
    /// The list of <typeparamref name="T"/> documents returned for this page.
    /// </summary>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// Timestamp of the oldest document in this page. Use as cursor for previous page.
    /// </summary>
    public DateTime? PrevCursor { get; set; }

    /// <summary>
    /// Timestamp of the newest document in this page. Use as cursor for next page.
    /// </summary>
    public DateTime? NextCursor { get; set; }
}
