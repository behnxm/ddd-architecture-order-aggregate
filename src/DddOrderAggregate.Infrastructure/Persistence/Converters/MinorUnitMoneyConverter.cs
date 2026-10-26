using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DddOrderAggregate.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts monetary decimal amounts to integer minor units (e.g. cents, pence) stored as 64-bit integers.
/// SQLite has dynamic column typing without a native high-precision decimal type.
/// Storing money as integer minor units ensures deterministic sorting, aggregation, and arithmetic at the database level.
/// </summary>
public sealed class MinorUnitMoneyConverter : ValueConverter<decimal, long>
{
    public MinorUnitMoneyConverter()
        : base(
            amount => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero),
            minorUnits => minorUnits / 100m)
    {
    }
}
