using System.Globalization;
using DddOrderAggregate.Domain.Orders.Errors;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Domain.Orders.ValueObjects;

/// <summary>
/// Immutable value object representing an item quantity constrained between 1 and 100.
/// </summary>
public readonly record struct Quantity
{
    public const int MinValue = 1;
    public const int MaxValue = 100;

    public int Value { get; }

    private Quantity(int value)
    {
        Value = value;
    }

    public static Result<Quantity> Create(int value)
    {
        if (value is < MinValue or > MaxValue)
        {
            return Result.Failure<Quantity>(OrderErrors.InvalidQuantity(value));
        }

        return Result.Success(new Quantity(value));
    }

    public Result<Quantity> Add(Quantity other)
    {
        var combined = Value + other.Value;
        if (combined > MaxValue)
        {
            return Result.Failure<Quantity>(OrderErrors.QuantityExceedsLimit(combined));
        }

        return Result.Success(new Quantity(combined));
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static implicit operator int(Quantity quantity) => quantity.Value;
}
