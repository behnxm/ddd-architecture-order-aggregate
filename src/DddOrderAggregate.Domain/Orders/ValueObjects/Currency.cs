using DddOrderAggregate.Domain.Orders.Errors;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Domain.Orders.ValueObjects;

/// <summary>
/// Immutable value object representing an ISO currency code supported by the Ordering domain.
/// </summary>
public readonly record struct Currency
{
    private static readonly HashSet<string> SupportedCurrencies = ["USD", "EUR", "GBP"];

    public static readonly Currency USD = new("USD");
    public static readonly Currency EUR = new("EUR");
    public static readonly Currency GBP = new("GBP");

    public string Code { get; }

    private Currency(string code)
    {
        Code = code;
    }

    public static Result<Currency> FromCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Currency>(OrderErrors.InvalidCurrency(code));
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (!SupportedCurrencies.Contains(normalized))
        {
            return Result.Failure<Currency>(OrderErrors.InvalidCurrency(code));
        }

        return Result.Success(new Currency(normalized));
    }

    public override string ToString() => Code;

    public static implicit operator string(Currency currency) => currency.Code;
}
