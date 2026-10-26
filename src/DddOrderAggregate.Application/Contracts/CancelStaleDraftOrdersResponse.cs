namespace DddOrderAggregate.Application.Contracts;

/// <summary>
/// Response payload indicating the count of stale draft orders cancelled during maintenance.
/// </summary>
public sealed record CancelStaleDraftOrdersResponse(int CancelledCount);
