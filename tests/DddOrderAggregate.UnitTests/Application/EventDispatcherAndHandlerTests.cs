using DddOrderAggregate.Application.Common;
using DddOrderAggregate.Application.IntegrationEvents;
using DddOrderAggregate.Application.Orders.EventHandlers;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders.Events;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DddOrderAggregate.UnitTests.Application;

public class EventDispatcherAndHandlerTests
{
    private readonly DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DomainEventDispatcher_InvokesRegisteredHandlers()
    {
        var handler = Substitute.For<IDomainEventHandler<OrderPaidDomainEvent>>();
        var services = new ServiceCollection();
        services.AddSingleton(handler);

        using var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(serviceProvider);

        var domainEvent = OrderPaidDomainEvent.Create(OrderId.New(), _now);
        await dispatcher.DispatchAsync(domainEvent);

        await handler.Received(1).HandleAsync(domainEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OrderSubmittedDomainEventHandler_TranslatesToIntegrationEventAndEnqueuesInOutbox()
    {
        var outbox = Substitute.For<IOutbox>();
        var handler = new OrderSubmittedDomainEventHandler(outbox, NullLogger<OrderSubmittedDomainEventHandler>.Instance);

        var orderId = OrderId.New();
        var customerId = CustomerId.New();
        var total = Money.Create(299.99m, Currency.USD).Value;

        var domainEvent = OrderSubmittedDomainEvent.Create(orderId, customerId, total, _now);
        await handler.HandleAsync(domainEvent);

        outbox.Received(1).Enqueue(Arg.Is<OrderSubmittedIntegrationEvent>(e =>
            e.OrderId == orderId.Value &&
            e.CustomerId == customerId.Value &&
            e.TotalAmount == 299.99m &&
            e.Currency == "USD" &&
            e.ContractName == OrderSubmittedIntegrationEvent.StableContractName));
    }

    [Fact]
    public async Task OrderCancelledDomainEventHandler_TranslatesToIntegrationEventAndEnqueuesInOutbox()
    {
        var outbox = Substitute.For<IOutbox>();
        var handler = new OrderCancelledDomainEventHandler(outbox, NullLogger<OrderCancelledDomainEventHandler>.Instance);

        var orderId = OrderId.New();
        var domainEvent = OrderCancelledDomainEvent.Create(orderId, "Customer duplicate order", _now);
        await handler.HandleAsync(domainEvent);

        outbox.Received(1).Enqueue(Arg.Is<OrderCancelledIntegrationEvent>(e =>
            e.OrderId == orderId.Value &&
            e.Reason == "Customer duplicate order" &&
            e.ContractName == OrderCancelledIntegrationEvent.StableContractName));
    }

    [Fact]
    public async Task OrderPaidAndShippedHandlers_ExecuteWithoutOutboxEnqueuing()
    {
        var paidHandler = new OrderPaidDomainEventHandler(NullLogger<OrderPaidDomainEventHandler>.Instance);
        var shippedHandler = new OrderShippedDomainEventHandler(NullLogger<OrderShippedDomainEventHandler>.Instance);

        var paidEvent = OrderPaidDomainEvent.Create(OrderId.New(), _now);
        var shippedEvent = OrderShippedDomainEvent.Create(OrderId.New(), _now);

        // Demonstrates local domain event handling that intentionally does not publish integration events
        await paidHandler.HandleAsync(paidEvent);
        await shippedHandler.HandleAsync(shippedEvent);
    }
}
