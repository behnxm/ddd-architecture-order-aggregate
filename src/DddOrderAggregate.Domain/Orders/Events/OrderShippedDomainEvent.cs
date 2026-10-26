using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel;

namespace DddOrderAggregate.Domain.Orders.Events;

/// <summary>
/// Domain event raised when an order has been dispatched from the warehouse.
/// </summary>
public sealed record OrderShippedDomainEvent(
    Guid EventId,
    OrderId OrderId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public static OrderShippedDomainEvent Create(OrderId orderId, DateTimeOffset occurredOn) =>
        new(Guid.CreateVersion7(), orderId, occurredOn);
}
