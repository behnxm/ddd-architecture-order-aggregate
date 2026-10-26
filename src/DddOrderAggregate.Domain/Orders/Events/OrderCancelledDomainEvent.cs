using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel;

namespace DddOrderAggregate.Domain.Orders.Events;

/// <summary>
/// Domain event raised when an order is cancelled prior to shipment.
/// </summary>
public sealed record OrderCancelledDomainEvent(
    Guid EventId,
    OrderId OrderId,
    string Reason,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public static OrderCancelledDomainEvent Create(OrderId orderId, string reason, DateTimeOffset occurredOn) =>
        new(Guid.CreateVersion7(), orderId, reason, occurredOn);
}
