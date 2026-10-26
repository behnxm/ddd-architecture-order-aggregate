using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel;

namespace DddOrderAggregate.Domain.Orders.Events;

/// <summary>
/// Domain event raised when a draft order satisfies all invariants and is submitted for fulfillment.
/// </summary>
public sealed record OrderSubmittedDomainEvent(
    Guid EventId,
    OrderId OrderId,
    CustomerId CustomerId,
    Money Total,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public static OrderSubmittedDomainEvent Create(OrderId orderId, CustomerId customerId, Money total, DateTimeOffset occurredOn) =>
        new(Guid.CreateVersion7(), orderId, customerId, total, occurredOn);
}
