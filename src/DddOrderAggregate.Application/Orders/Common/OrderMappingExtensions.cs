using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Domain.Orders;

namespace DddOrderAggregate.Application.Orders.Common;

/// <summary>
/// Extension methods for mapping domain models to primitive application DTOs.
/// </summary>
public static class OrderMappingExtensions
{
    public static OrderResponse ToResponse(this Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var address = order.ShippingAddress is null
            ? null
            : new AddressResponse(
                order.ShippingAddress.Street,
                order.ShippingAddress.City,
                order.ShippingAddress.PostalCode,
                order.ShippingAddress.CountryCode);

        var lines = order.Lines
            .Select(l => new OrderLineResponse(
                l.Id.Value,
                l.ProductId.Value,
                l.ProductName,
                l.UnitPrice.Amount,
                l.Quantity.Value,
                l.LineTotal.Amount))
            .ToList();

        return new OrderResponse(
            order.Id.Value,
            order.CustomerId.Value,
            order.Status.ToString(),
            order.Currency.Code,
            order.Total.Amount,
            address,
            lines,
            order.CreatedAt,
            order.UpdatedAt,
            order.SubmittedAt,
            order.PaidAt,
            order.ShippedAt,
            order.CancelledAt,
            order.CancellationReason,
            order.Version);
    }
}
