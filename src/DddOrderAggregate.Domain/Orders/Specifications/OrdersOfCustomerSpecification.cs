using System.Linq.Expressions;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel.Specifications;

namespace DddOrderAggregate.Domain.Orders.Specifications;

/// <summary>
/// Filters orders belonging to a specific customer.
/// </summary>
public sealed class OrdersOfCustomerSpecification : Specification<Order>
{
    private readonly CustomerId _customerId;

    public OrdersOfCustomerSpecification(CustomerId customerId)
    {
        _customerId = customerId;
    }

    public override Expression<Func<Order, bool>> Criteria =>
        order => order.CustomerId == _customerId;
}
