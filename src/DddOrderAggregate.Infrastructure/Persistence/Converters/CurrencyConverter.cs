using DddOrderAggregate.Domain.Orders.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DddOrderAggregate.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts Currency value objects to and from a 3-character ISO string representation.
/// </summary>
public sealed class CurrencyConverter : ValueConverter<Currency, string>
{
    public CurrencyConverter()
        : base(currency => currency.Code, code => Currency.FromCode(code).Value)
    {
    }
}
