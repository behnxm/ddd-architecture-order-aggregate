using System.Linq.Expressions;
using DddOrderAggregate.Domain.SharedKernel.Specifications;

namespace DddOrderAggregate.Domain.Orders.Specifications;

/// <summary>
/// Identifies draft orders that have not been modified since a specified cutoff threshold.
/// Serves as the single source of truth for stale draft evaluation in both write repositories,
/// read queries, and in-memory aggregate evaluation.
/// </summary>
public sealed class StaleDraftOrdersSpecification : Specification<Order>
{
    private readonly DateTimeOffset _cutoff;

    public StaleDraftOrdersSpecification(DateTimeOffset cutoff)
    {
        _cutoff = cutoff;
    }

    public override Expression<Func<Order, bool>> Criteria =>
        order => order.Status == OrderStatus.Draft && order.UpdatedAt < _cutoff;
}
