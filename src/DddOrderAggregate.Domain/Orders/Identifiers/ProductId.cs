using DddOrderAggregate.Domain.SharedKernel;

namespace DddOrderAggregate.Domain.Orders.Identifiers;

/// <summary>
/// Strongly-typed external identifier referencing a catalog product.
/// </summary>
public readonly record struct ProductId(Guid Value) : IStronglyTypedId<ProductId>
{
    public static ProductId New() => new(Guid.CreateVersion7());

    public static ProductId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("ProductId cannot be empty.", nameof(value));
        }

        return new(value);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(ProductId id) => id.Value;
}
