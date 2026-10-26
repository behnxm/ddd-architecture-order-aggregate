using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel;

namespace DddOrderAggregate.Domain.Orders.Events;

/// <summary>
/// Domain event raised when payment settlement is recorded for a submitted order.
/// </summary>
public sealed record OrderPaidDomainEvent(
    Guid EventId,
    OrderId OrderId,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public static OrderPaidDomainEvent Create(OrderId orderId, DateTimeOffset occurredOn) =>
        new(Guid.CreateVersion7(), orderId, occurredOn);
}
