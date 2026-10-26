using DddOrderAggregate.Application.Contracts;
using DddOrderAggregate.Application.Orders.Commands;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Domain.SharedKernel.Results;
using DddOrderAggregate.Domain.SharedKernel.Specifications;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DddOrderAggregate.UnitTests.Application;

public class CommandHandlerTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IProductCatalog _catalog = Substitute.For<IProductCatalog>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task CreateOrder_HappyPath_CreatesOrderAndSaves()
    {
        var handler = new CreateOrderCommandHandler(_repository, _unitOfWork, _timeProvider);
        var customerId = Guid.NewGuid();

        var result = await handler.HandleAsync(new CreateOrderCommand(customerId, "USD"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.CustomerId.ShouldBe(customerId);
        result.Value.Currency.ShouldBe("USD");
        result.Value.Status.ShouldBe("Draft");
        result.Value.Version.ShouldBe(1);

        await _repository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOrder_InvalidCurrency_ReturnsValidationError()
    {
        var handler = new CreateOrderCommandHandler(_repository, _unitOfWork, _timeProvider);
        var result = await handler.HandleAsync(new CreateOrderCommand(Guid.NewGuid(), "INVALID"));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Currency.Invalid");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrderLine_HappyPath_AddsLineAndSaves()
    {
        var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _timeProvider.GetUtcNow()).Value;
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var productId = ProductId.New();
        var snapshot = new ProductSnapshot(productId, "Keyboard", Money.Create(100m, Currency.USD).Value);
        _catalog.GetProductAsync(productId, Arg.Any<CancellationToken>()).Returns(Result.Success(snapshot));

        var handler = new AddOrderLineCommandHandler(_repository, _unitOfWork, _catalog, _timeProvider);
        var result = await handler.HandleAsync(new AddOrderLineCommand(order.Id.Value, productId.Value, 2, ExpectedVersion: 1));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Lines.Count.ShouldBe(1);
        result.Value.Total.ShouldBe(200m);
        result.Value.Version.ShouldBe(2);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrderLine_OrderNotFound_ReturnsNotFoundError()
    {
        var orderId = OrderId.New();
        _repository.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((Order?)null);

        var productId = ProductId.New();
        var snapshot = new ProductSnapshot(productId, "Keyboard", Money.Create(100m, Currency.USD).Value);
        _catalog.GetProductAsync(productId, Arg.Any<CancellationToken>()).Returns(Result.Success(snapshot));

        var handler = new AddOrderLineCommandHandler(_repository, _unitOfWork, _catalog, _timeProvider);
        var result = await handler.HandleAsync(new AddOrderLineCommand(orderId.Value, productId.Value, 1));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Order.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrderLine_VersionMismatch_ReturnsPreconditionFailed()
    {
        var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _timeProvider.GetUtcNow()).Value;
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var productId = ProductId.New();
        var snapshot = new ProductSnapshot(productId, "Keyboard", Money.Create(100m, Currency.USD).Value);
        _catalog.GetProductAsync(productId, Arg.Any<CancellationToken>()).Returns(Result.Success(snapshot));

        var handler = new AddOrderLineCommandHandler(_repository, _unitOfWork, _catalog, _timeProvider);
        // Current version is 1, expected is 99
        var result = await handler.HandleAsync(new AddOrderLineCommand(order.Id.Value, productId.Value, 1, ExpectedVersion: 99));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Order.VersionMismatch");
        result.Error.Type.ShouldBe(ErrorType.PreconditionFailed);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrderLine_CurrencyMismatch_ReturnsValidationError()
    {
        var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _timeProvider.GetUtcNow()).Value;
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var productId = ProductId.New();
        // Catalog returns EUR product for USD order
        var snapshot = new ProductSnapshot(productId, "Euro Plug", Money.Create(20m, Currency.EUR).Value);
        _catalog.GetProductAsync(productId, Arg.Any<CancellationToken>()).Returns(Result.Success(snapshot));

        var handler = new AddOrderLineCommandHandler(_repository, _unitOfWork, _catalog, _timeProvider);
        var result = await handler.HandleAsync(new AddOrderLineCommand(order.Id.Value, productId.Value, 1));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Order.CurrencyMismatch");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitOrder_HappyPath_SubmitsAndSaves()
    {
        var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _timeProvider.GetUtcNow()).Value;
        order.AddLine(ProductId.New(), "Item", Money.Create(50m, Currency.USD).Value, Quantity.Create(1).Value, _timeProvider.GetUtcNow());
        order.SetShippingAddress(Address.Create("123 St", "City", "12345", "US").Value, _timeProvider.GetUtcNow());

        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new SubmitOrderCommandHandler(_repository, _unitOfWork, _timeProvider);
        var result = await handler.HandleAsync(new SubmitOrderCommand(order.Id.Value));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe("Submitted");
        result.Value.SubmittedAt.ShouldNotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelOrder_HappyPath_CancelsAndSaves()
    {
        var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _timeProvider.GetUtcNow()).Value;
        _repository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new CancelOrderCommandHandler(_repository, _unitOfWork, _timeProvider);
        var result = await handler.HandleAsync(new CancelOrderCommand(order.Id.Value, "User requested refund"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe("Cancelled");
        result.Value.CancellationReason.ShouldBe("User requested refund");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelStaleDraftOrders_CancelsMatchingOrders()
    {
        var oldTime = _timeProvider.GetUtcNow().AddHours(-48);
        var order1 = Order.CreateDraft(CustomerId.New(), Currency.USD, oldTime).Value;
        var order2 = Order.CreateDraft(CustomerId.New(), Currency.USD, oldTime).Value;

        _repository.ListAsync(Arg.Any<ISpecification<Order>>(), Arg.Any<CancellationToken>())
            .Returns([order1, order2]);

        var handler = new CancelStaleDraftOrdersCommandHandler(_repository, _unitOfWork, _timeProvider);
        var result = await handler.HandleAsync(new CancelStaleDraftOrdersCommand(24));

        result.IsSuccess.ShouldBeTrue();
        result.Value.CancelledCount.ShouldBe(2);

        order1.Status.ShouldBe(OrderStatus.Cancelled);
        order2.Status.ShouldBe(OrderStatus.Cancelled);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
