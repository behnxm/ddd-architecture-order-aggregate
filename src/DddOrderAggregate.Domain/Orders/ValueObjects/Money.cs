using System.Globalization;
using DddOrderAggregate.Domain.Orders.Errors;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Domain.Orders.ValueObjects;

/// <summary>
/// Immutable value object representing a monetary quantity with an explicit currency.
/// Enforces non-negative amounts, at most two decimal places, and currency congruence during arithmetic.
/// Prevents bypass of invariants by hiding copy constructors against with-expressions.
/// </summary>
public sealed record Money : IComparable<Money>
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // Private copy constructor prevents external 'with' expressions from bypassing domain validation
    private Money(Money original)
    {
        Amount = original.Amount;
        Currency = original.Currency;
    }

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (amount < 0m || decimal.Round(amount, 2) != amount)
        {
            return Result.Failure<Money>(OrderErrors.InvalidAmount(amount));
        }

        return Result.Success(new Money(amount, currency));
    }

    public static Money Zero(Currency currency) => new(0.00m, currency);

    public Result<Money> Add(Money other)
    {
        if (Currency != other.Currency)
        {
            return Result.Failure<Money>(OrderErrors.CurrencyMismatch(Currency.Code, other.Currency.Code));
        }

        return Create(Amount + other.Amount, Currency);
    }

    public Money Multiply(Quantity quantity)
    {
        // Multiplying a 2-decimal non-negative decimal by an integer 1..100 preserves exact 2-decimal precision
        return new Money(Amount * quantity.Value, Currency);
    }

    public int CompareTo(Money? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (Currency != other.Currency)
        {
            throw new InvalidOperationException($"Cannot compare money in different currencies: '{Currency}' and '{other.Currency}'.");
        }

        return Amount.CompareTo(other.Amount);
    }

    public static bool operator <(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.CompareTo(right) >= 0;
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency.Code}");
}
