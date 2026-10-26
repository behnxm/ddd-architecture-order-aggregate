using DddOrderAggregate.Domain.SharedKernel;

namespace DddOrderAggregate.Domain.Orders.Identifiers;

/// <summary>
/// Strongly-typed identifier for an internal OrderLine entity.
/// </summary>
public readonly record struct OrderLineId(Guid Value) : IStronglyTypedId<OrderLineId>
{
    public static OrderLineId New() => new(Guid.CreateVersion7());

    public static OrderLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("OrderLineId cannot be empty.", nameof(value));
        }

        return new(value);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(OrderLineId id) => id.Value;
}
