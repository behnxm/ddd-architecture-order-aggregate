using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel.Specifications;
using DddOrderAggregate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DddOrderAggregate.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for the Order aggregate root.
/// Operates on the full aggregate consistency boundary; child lines are loaded automatically.
/// Evaluates domain specifications directly against LINQ query providers.
/// </summary>
public sealed class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _dbContext;

    public OrderRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        await _dbContext.Orders.AddAsync(order, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Order>> ListAsync(
        ISpecification<Order> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        return await _dbContext.Orders
            .Where(specification.Criteria)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
