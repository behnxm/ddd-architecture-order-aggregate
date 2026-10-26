using DddOrderAggregate.Domain.Orders.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DddOrderAggregate.Infrastructure.Persistence.Converters;

/// <summary>
/// Converts Quantity value objects to and from integer database columns.
/// </summary>
public sealed class QuantityConverter : ValueConverter<Quantity, int>
{
    public QuantityConverter()
        : base(quantity => quantity.Value, value => Quantity.Create(value).Value)
    {
    }
}
