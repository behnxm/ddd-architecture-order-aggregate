using DddOrderAggregate.Application.Contracts;

namespace DddOrderAggregate.Application.Ports;

/// <summary>
/// Read-side query port.
/// Projects database records directly to DTO responses, bypassing aggregate loading and tracking overhead.
/// </summary>
public interface IOrderReadRepository
{
    /// <summary>
    /// Gets an order by its ID, projecting directly to OrderResponse.
    /// </summary>
    Task<OrderResponse?> GetOrderAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries orders with optional customer and status filters, returning a paginated summary list.
    /// </summary>
    Task<PagedList<OrderSummaryResponse>> ListOrdersAsync(
        Guid? customerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
