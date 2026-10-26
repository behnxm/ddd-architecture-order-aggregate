using DddOrderAggregate.Domain.SharedKernel;

namespace DddOrderAggregate.Domain.Orders.Identifiers;

/// <summary>
/// Strongly-typed external identifier referencing a customer in an external context.
/// </summary>
public readonly record struct CustomerId(Guid Value) : IStronglyTypedId<CustomerId>
{
    public static CustomerId New() => new(Guid.CreateVersion7());

    public static CustomerId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("CustomerId cannot be empty.", nameof(value));
        }

        return new(value);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(CustomerId id) => id.Value;
}
