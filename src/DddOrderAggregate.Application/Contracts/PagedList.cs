namespace DddOrderAggregate.Application.Contracts;

/// <summary>
/// Generic container for paginated query results.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
