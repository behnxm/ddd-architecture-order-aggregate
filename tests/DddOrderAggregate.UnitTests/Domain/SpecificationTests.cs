using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.Specifications;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using Shouldly;
using Xunit;

namespace DddOrderAggregate.UnitTests.Domain;

public class SpecificationTests
{
    private readonly DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Specification_EvaluatesIsSatisfiedBy()
    {
        var targetCustomer = CustomerId.New();
        var otherCustomer = CustomerId.New();

        var order = Order.CreateDraft(targetCustomer, Currency.USD, _now).Value;

        var spec = new OrdersOfCustomerSpecification(targetCustomer);
        var otherSpec = new OrdersOfCustomerSpecification(otherCustomer);

        spec.IsSatisfiedBy(order).ShouldBeTrue();
        otherSpec.IsSatisfiedBy(order).ShouldBeFalse();
    }

    [Fact]
    public void Specification_SupportsAndComposition()
    {
        var customerId = CustomerId.New();
        var order = Order.CreateDraft(customerId, Currency.USD, _now).Value;

        var customerSpec = new OrdersOfCustomerSpecification(customerId);
        var draftSpec = new OrdersInStatusSpecification(OrderStatus.Draft);
        var submittedSpec = new OrdersInStatusSpecification(OrderStatus.Submitted);

        var combinedSpec = customerSpec.And(draftSpec);
        combinedSpec.IsSatisfiedBy(order).ShouldBeTrue();

        var failingSpec = customerSpec.And(submittedSpec);
        failingSpec.IsSatisfiedBy(order).ShouldBeFalse();
    }

    [Fact]
    public void Specification_SupportsOrComposition()
    {
        var customerId = CustomerId.New();
        var order = Order.CreateDraft(customerId, Currency.USD, _now).Value;

        var submittedSpec = new OrdersInStatusSpecification(OrderStatus.Submitted);
        var draftSpec = new OrdersInStatusSpecification(OrderStatus.Draft);

        var orSpec = submittedSpec.Or(draftSpec);
        orSpec.IsSatisfiedBy(order).ShouldBeTrue();

        var otherCustomerSpec = new OrdersOfCustomerSpecification(CustomerId.New());
        var paidSpec = new OrdersInStatusSpecification(OrderStatus.Paid);

        var failingOr = otherCustomerSpec.Or(paidSpec);
        failingOr.IsSatisfiedBy(order).ShouldBeFalse();
    }

    [Fact]
    public void Specification_SupportsNotComposition()
    {
        var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _now).Value;
        var submittedSpec = new OrdersInStatusSpecification(OrderStatus.Submitted);

        var notSubmittedSpec = submittedSpec.Not();
        notSubmittedSpec.IsSatisfiedBy(order).ShouldBeTrue();

        var draftSpec = new OrdersInStatusSpecification(OrderStatus.Draft);
        var notDraftSpec = draftSpec.Not();
        notDraftSpec.IsSatisfiedBy(order).ShouldBeFalse();
    }
}
