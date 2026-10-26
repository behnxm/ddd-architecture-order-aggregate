using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.Specifications;
using DddOrderAggregate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DddOrderAggregate.Infrastructure.ReadModels;

/// <summary>
/// Read repository projecting directly from the relational database into lightweight DTOs.
/// Uses AsNoTracking and reuses the exact same domain specification expression trees.
/// </summary>
public sealed class OrderReadRepository : IOrderReadRepository
{
    private readonly ApplicationDbContext _dbContext;

    public OrderReadRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderResponse?> GetOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var orderId = OrderId.From(id);

        return await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new OrderResponse(
                o.Id.Value,
                o.CustomerId.Value,
                o.Status.ToString(),
                o.Currency.Code,
                o.Total.Amount,
                o.ShippingAddress == null
                    ? null
                    : new AddressResponse(
                        o.ShippingAddress.Street,
                        o.ShippingAddress.City,
                        o.ShippingAddress.PostalCode,
                        o.ShippingAddress.CountryCode),
                o.Lines.Select(l => new OrderLineResponse(
                    l.Id.Value,
                    l.ProductId.Value,
                    l.ProductName,
                    l.UnitPrice.Amount,
                    l.Quantity.Value,
                    l.LineTotal.Amount)).ToList(),
                o.CreatedAt,
                o.UpdatedAt,
                o.SubmittedAt,
                o.PaidAt,
                o.ShippedAt,
                o.CancelledAt,
                o.CancellationReason,
                o.Version))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PagedList<OrderSummaryResponse>> ListOrdersAsync(
        Guid? customerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders.AsNoTracking();

        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            var customerSpec = new OrdersOfCustomerSpecification(CustomerId.From(customerId.Value));
            query = query.Where(customerSpec.Criteria);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var orderStatus))
        {
            var statusSpec = new OrdersInStatusSpecification(orderStatus);
            query = query.Where(statusSpec.Criteria);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryResponse(
                o.Id.Value,
                o.CustomerId.Value,
                o.Status.ToString(),
                o.Currency.Code,
                o.Total.Amount,
                o.Lines.Count,
                o.CreatedAt,
                o.UpdatedAt,
                o.Version))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedList<OrderSummaryResponse>(items, page, pageSize, totalCount);
    }
}
