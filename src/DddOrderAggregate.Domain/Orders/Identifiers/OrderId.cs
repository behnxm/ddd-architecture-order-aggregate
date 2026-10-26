using DddOrderAggregate.Domain.SharedKernel;

namespace DddOrderAggregate.Domain.Orders.Identifiers;

/// <summary>
/// Strongly-typed identifier for an Order aggregate root.
/// </summary>
public readonly record struct OrderId(Guid Value) : IStronglyTypedId<OrderId>
{
    public static OrderId New() => new(Guid.CreateVersion7());

    public static OrderId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("OrderId cannot be empty.", nameof(value));
        }

        return new(value);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(OrderId id) => id.Value;
}
