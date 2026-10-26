namespace DddOrderAggregate.Application.Contracts;

/// <summary>
/// Compact projection used for paginated listing of orders.
/// </summary>
public sealed record OrderSummaryResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    string Currency,
    decimal TotalAmount,
    int LinesCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version);
