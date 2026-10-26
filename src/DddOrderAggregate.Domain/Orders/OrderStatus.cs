namespace DddOrderAggregate.Domain.Orders;

/// <summary>
/// Lifecycle statuses for the Order aggregate root.
/// </summary>
public enum OrderStatus
{
    Draft = 1,
    Submitted = 2,
    Paid = 3,
    Shipped = 4,
    Cancelled = 5
}
