using System.Linq.Expressions;
using DddOrderAggregate.Domain.SharedKernel.Specifications;

namespace DddOrderAggregate.Domain.Orders.Specifications;

/// <summary>
/// Filters orders currently in a specific lifecycle status.
/// </summary>
public sealed class OrdersInStatusSpecification : Specification<Order>
{
    private readonly OrderStatus _status;

    public OrdersInStatusSpecification(OrderStatus status)
    {
        _status = status;
    }

    public override Expression<Func<Order, bool>> Criteria =>
        order => order.Status == _status;
}
