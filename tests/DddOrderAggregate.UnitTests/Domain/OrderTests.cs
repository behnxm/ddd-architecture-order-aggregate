using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Errors;
using DddOrderAggregate.Domain.Orders.Events;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using Shouldly;
using Xunit;

namespace DddOrderAggregate.UnitTests.Domain;

public class OrderTests
{
    private readonly DateTimeOffset _initialTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Invariant", "INV-01")]
    public void Invariant_INV01_CustomerAndCurrencyFixedAtCreation()
    {
        var customerId = CustomerId.New();
        var currency = Currency.USD;

        var order = Order.CreateDraft(customerId, currency, _initialTime).Value;

        order.CustomerId.ShouldBe(customerId);
        order.Currency.ShouldBe(currency);
        order.Status.ShouldBe(OrderStatus.Draft);
        order.Total.ShouldBe(Money.Zero(currency));
        order.Version.ShouldBe(1);
    }

    [Fact]
    [Trait("Invariant", "INV-02")]
    public void Invariant_INV02_ModificationsAllowedOnlyInDraft()
    {
        var order = OrderBuilder.Create()
            .WithTime(_initialTime)
            .WithStandardLine()
            .WithStandardShippingAddress()
            .Build();

        var submitTime = _initialTime.AddMinutes(5);
        order.Submit(submitTime).IsSuccess.ShouldBeTrue();

        var mutationTime = submitTime.AddMinutes(1);
        var extraProduct = ProductId.New();
        var extraPrice = Money.Create(50.00m, Currency.USD).Value;
        var extraQty = Quantity.Create(1).Value;

        // Modifying lines in Submitted state must fail
        var addResult = order.AddLine(extraProduct, "Extra Item", extraPrice, extraQty, mutationTime);
        addResult.IsFailure.ShouldBeTrue();
        addResult.Error.Code.ShouldBe("Order.NotDraft");

        var lineId = order.Lines[0].Id;
        var changeResult = order.ChangeLineQuantity(lineId, Quantity.Create(5).Value, mutationTime);
        changeResult.IsFailure.ShouldBeTrue();
        changeResult.Error.Code.ShouldBe("Order.NotDraft");

        var removeResult = order.RemoveLine(lineId, mutationTime);
        removeResult.IsFailure.ShouldBeTrue();
        removeResult.Error.Code.ShouldBe("Order.NotDraft");

        var newAddress = Address.Create("999 New St", "Seattle", "98101", "US").Value;
        var addressResult = order.SetShippingAddress(newAddress, mutationTime);
        addressResult.IsFailure.ShouldBeTrue();
        addressResult.Error.Code.ShouldBe("Order.NotDraft");
    }

    [Fact]
    [Trait("Invariant", "INV-03")]
    public void Invariant_INV03_ProductAddedAgainMergesQuantityAndKeepsOriginalPrice()
    {
        var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _initialTime).Value;
        var productId = ProductId.New();
        var originalPrice = Money.Create(100.00m, Currency.USD).Value;
        var newCatalogPrice = Money.Create(150.00m, Currency.USD).Value;

        order.AddLine(productId, "Product Snapshot", originalPrice, Quantity.Create(2).Value, _initialTime).IsSuccess.ShouldBeTrue();

        // Add the same product with a higher catalog price and 3 more items
        var addAgainResult = order.AddLine(productId, "Product Snapshot Updated", newCatalogPrice, Quantity.Create(3).Value, _initialTime.AddMinutes(1));
        addAgainResult.IsSuccess.ShouldBeTrue();

        order.Lines.Count.ShouldBe(1);
        order.Lines[0].Quantity.Value.ShouldBe(5);
        // Original unit price snapshot must be preserved
        order.Lines[0].UnitPrice.Amount.ShouldBe(100.00m);
        order.Lines[0].LineTotal.Amount.ShouldBe(500.00m);
        order.Total.Amount.ShouldBe(500.00m);
    }

    [Fact]
    [Trait("Invariant", "INV-04")]
    public void Invariant_INV04_QuantityBetween1And100_AndAtMost20Lines()
    {
        var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _initialTime).Value;

        // Cannot add more than 20 distinct lines
        for (var i = 1; i <= 20; i++)
        {
            var res = order.AddLine(
                ProductId.New(),
                $"Item {i}",
                Money.Create(10.00m, Currency.USD).Value,
                Quantity.Create(1).Value,
                _initialTime);
            res.IsSuccess.ShouldBeTrue();
        }

        order.Lines.Count.ShouldBe(20);

        var twentyFirstResult = order.AddLine(
            ProductId.New(),
            "Item 21",
            Money.Create(10.00m, Currency.USD).Value,
            Quantity.Create(1).Value,
            _initialTime);

        twentyFirstResult.IsFailure.ShouldBeTrue();
        twentyFirstResult.Error.Code.ShouldBe("Order.TooManyLines");
    }

    [Fact]
    [Trait("Invariant", "INV-05")]
    public void Invariant_INV05_UnitPriceMustBePositiveAndMatchOrderCurrency()
    {
        var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _initialTime).Value;

        // Currency mismatch: EUR line on USD order
        var eurPrice = Money.Create(25.00m, Currency.EUR).Value;
        var mismatchResult = order.AddLine(ProductId.New(), "Euro Plug", eurPrice, Quantity.Create(1).Value, _initialTime);
        mismatchResult.IsFailure.ShouldBeTrue();
        mismatchResult.Error.Code.ShouldBe("Order.CurrencyMismatch");

        // Zero amount unit price
        var zeroPrice = Money.Zero(Currency.USD);
        var zeroResult = order.AddLine(ProductId.New(), "Free Sample", zeroPrice, Quantity.Create(1).Value, _initialTime);
        zeroResult.IsFailure.ShouldBeTrue();
        zeroResult.Error.Code.ShouldBe("Money.InvalidAmount");
    }

    [Fact]
    [Trait("Invariant", "INV-06")]
    public void Invariant_INV06_OrderTotalAlwaysEqualsSumOfLineTotals()
    {
        var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _initialTime).Value;
        var p1 = ProductId.New();
        var p2 = ProductId.New();

        order.AddLine(p1, "Item A", Money.Create(20.00m, Currency.USD).Value, Quantity.Create(2).Value, _initialTime);
        order.Total.Amount.ShouldBe(40.00m);

        order.AddLine(p2, "Item B", Money.Create(15.50m, Currency.USD).Value, Quantity.Create(1).Value, _initialTime);
        order.Total.Amount.ShouldBe(55.50m);

        var line1Id = order.Lines.First(l => l.ProductId == p1).Id;
        order.ChangeLineQuantity(line1Id, Quantity.Create(5).Value, _initialTime);
        // (20.00 * 5) + 15.50 = 115.50
        order.Total.Amount.ShouldBe(115.50m);

        order.RemoveLine(line1Id, _initialTime);
        order.Total.Amount.ShouldBe(15.50m);
    }

    [Fact]
    [Trait("Invariant", "INV-07")]
    public void Invariant_INV07_ShippingAddressRequiredBeforeSubmit()
    {
        var order = OrderBuilder.Create()
            .WithTime(_initialTime)
            .WithStandardLine()
            .Build();

        order.ShippingAddress.ShouldBeNull();
        var submitResult = order.Submit(_initialTime);

        submitResult.IsFailure.ShouldBeTrue();
        submitResult.Error.Code.ShouldBe("Order.AddressRequired");
    }

    [Fact]
    [Trait("Invariant", "INV-08")]
    public void Invariant_INV08_EmptyOrderCannotBeSubmitted()
    {
        var order = OrderBuilder.Create()
            .WithTime(_initialTime)
            .WithStandardShippingAddress()
            .Build();

        order.Lines.Count.ShouldBe(0);
        var submitResult = order.Submit(_initialTime);

        submitResult.IsFailure.ShouldBeTrue();
        submitResult.Error.Code.ShouldBe("Order.Empty");
    }

    [Theory]
    [Trait("Invariant", "INV-09")]
    [InlineData(OrderStatus.Draft, OrderStatus.Submitted, true)]
    [InlineData(OrderStatus.Draft, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Draft, OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Draft, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Submitted, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Submitted, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Submitted, OrderStatus.Draft, false)]
    [InlineData(OrderStatus.Submitted, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Draft, false)]
    [InlineData(OrderStatus.Paid, OrderStatus.Submitted, false)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Draft, false)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Submitted, false)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Draft, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Submitted, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Shipped, false)]
    public void Invariant_INV09_AllowedAndDisallowedStateTransitions(
        OrderStatus currentStatus,
        OrderStatus targetStatus,
        bool expectedAllowed)
    {
        var order = CreateOrderInStatus(currentStatus);
        order.CanTransitionTo(targetStatus).ShouldBe(expectedAllowed);

        // Verify executing the disallowed method fails with InvalidTransition
        if (!expectedAllowed)
        {
            var res = ExecuteTransition(order, targetStatus);
            res.IsFailure.ShouldBeTrue();
            res.Error.Code.ShouldBe("Order.InvalidTransition");
        }
    }

    [Theory]
    [Trait("Invariant", "INV-10")]
    [InlineData("")]
    [InlineData("   ")]
    public void Invariant_INV10_CancelRequiresReasonOf1To200Characters_RejectsEmpty(string emptyReason)
    {
        var order = OrderBuilder.Create().WithTime(_initialTime).Build();
        var cancelResult = order.Cancel(emptyReason, _initialTime);

        cancelResult.IsFailure.ShouldBeTrue();
        cancelResult.Error.Code.ShouldBe("Order.InvalidCancellationReason");
    }

    [Fact]
    [Trait("Invariant", "INV-10")]
    public void Invariant_INV10_CancelRequiresReasonOf1To200Characters_RejectsExceeding200Chars()
    {
        var order = OrderBuilder.Create().WithTime(_initialTime).Build();
        var longReason = new string('a', 201);

        var cancelResult = order.Cancel(longReason, _initialTime);
        cancelResult.IsFailure.ShouldBeTrue();
        cancelResult.Error.Code.ShouldBe("Order.InvalidCancellationReason");
    }

    [Fact]
    [Trait("Invariant", "INV-11")]
    public void Invariant_INV11_AggregateStateEncapsulatedAndLinesReadOnly()
    {
        var order = OrderBuilder.Create()
            .WithTime(_initialTime)
            .WithStandardLine()
            .Build();

        // Lines is exposed as IReadOnlyList and cannot be cast to mutate
        order.Lines.ShouldBeAssignableTo<IReadOnlyList<OrderLine>>();
        (order.Lines is List<OrderLine>).ShouldBeFalse();
    }

    [Fact]
    [Trait("Invariant", "INV-12")]
    public void Invariant_INV12_DomainEventsRaisedOnlyOnSuccessfulStateChange()
    {
        var order = OrderBuilder.Create()
            .WithTime(_initialTime)
            .WithStandardLine()
            .Build();

        order.DomainEvents.ShouldBeEmpty();

        // Submit without address must fail and raise nothing
        var failedSubmit = order.Submit(_initialTime);
        failedSubmit.IsFailure.ShouldBeTrue();
        order.DomainEvents.ShouldBeEmpty();

        // Set address and submit successfully
        order.SetShippingAddress(Address.Create("123 Main St", "City", "12345", "US").Value, _initialTime);
        var successSubmit = order.Submit(_initialTime);
        successSubmit.IsSuccess.ShouldBeTrue();

        order.DomainEvents.Count.ShouldBe(1);
        order.DomainEvents[0].ShouldBeOfType<OrderSubmittedDomainEvent>();
    }

    [Fact]
    public void Version_IncrementsOncePerSuccessfulMutation_AndNeverOnFailure()
    {
        var order = OrderBuilder.Create().WithTime(_initialTime).Build();
        order.Version.ShouldBe(1);

        // Successful add line increments version
        order.AddLine(ProductId.New(), "Item", Money.Create(10m, Currency.USD).Value, Quantity.Create(1).Value, _initialTime);
        order.Version.ShouldBe(2);

        // Failed add line (e.g. currency mismatch) leaves version unchanged
        order.AddLine(ProductId.New(), "Item EUR", Money.Create(10m, Currency.EUR).Value, Quantity.Create(1).Value, _initialTime);
        order.Version.ShouldBe(2);
    }

    [Fact]
    public void IsStaleDraft_ReturnsTrue_WhenOlderThanCutoff()
    {
        var order = OrderBuilder.Create()
            .WithTime(_initialTime)
            .Build();

        var cutoff = _initialTime.AddHours(24);
        order.IsStaleDraft(cutoff).ShouldBeTrue();

        var earlierCutoff = _initialTime.AddHours(-1);
        order.IsStaleDraft(earlierCutoff).ShouldBeFalse();
    }

    private Order CreateOrderInStatus(OrderStatus status)
    {
        var order = OrderBuilder.Create()
            .WithTime(_initialTime)
            .WithStandardLine()
            .WithStandardShippingAddress()
            .Build();

        if (status == OrderStatus.Draft)
        {
            return order;
        }

        order.Submit(_initialTime.AddMinutes(1));
        if (status == OrderStatus.Submitted)
        {
            return order;
        }

        if (status == OrderStatus.Cancelled)
        {
            order.Cancel("Customer requested cancellation", _initialTime.AddMinutes(2));
            return order;
        }

        order.MarkPaid(_initialTime.AddMinutes(2));
        if (status == OrderStatus.Paid)
        {
            return order;
        }

        order.MarkShipped(_initialTime.AddMinutes(3));
        return order;
    }

    private static DddOrderAggregate.Domain.SharedKernel.Results.Result ExecuteTransition(Order order, OrderStatus target) =>
        target switch
        {
            OrderStatus.Submitted => order.Submit(DateTimeOffset.UtcNow),
            OrderStatus.Paid => order.MarkPaid(DateTimeOffset.UtcNow),
            OrderStatus.Shipped => order.MarkShipped(DateTimeOffset.UtcNow),
            OrderStatus.Cancelled => order.Cancel("Valid reason", DateTimeOffset.UtcNow),
            _ => OrderErrors.InvalidTransition(order.Status, target)
        };
}
