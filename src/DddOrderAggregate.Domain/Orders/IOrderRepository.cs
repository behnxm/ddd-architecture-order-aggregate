using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel.Specifications;

namespace DddOrderAggregate.Domain.Orders;

/// <summary>
/// Repository interface for the Order aggregate root.
/// In Domain-Driven Design, repositories exist ONLY for aggregate roots.
/// Internal entities such as OrderLine have no repository and can only be accessed through Order.
/// Deletions are forbidden: orders are cancelled through domain state transitions, never hard-deleted.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Loads an order aggregate root by its identifier, including all of its internal lines.
    /// </summary>
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new order aggregate root to the underlying change tracker.
    /// </summary>
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a domain specification and retrieves matching aggregate roots with full internal state.
    /// </summary>
    Task<IReadOnlyList<Order>> ListAsync(ISpecification<Order> specification, CancellationToken cancellationToken = default);
}
