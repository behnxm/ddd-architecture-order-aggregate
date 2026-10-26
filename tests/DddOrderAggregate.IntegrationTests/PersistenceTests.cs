using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Errors;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.Specifications;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Infrastructure.Catalog;
using DddOrderAggregate.Infrastructure.Outbox;
using DddOrderAggregate.Infrastructure.Persistence;
using DddOrderAggregate.Infrastructure.Repositories;
using DddOrderAggregate.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace DddOrderAggregate.IntegrationTests;

public class PersistenceTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public PersistenceTests()
    {
        _factory = new CustomWebApplicationFactory();
    }

    public async ValueTask InitializeAsync()
    {
        await _factory.EnsureDatabaseCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PersistenceRoundTrip_PreservesAllAggregateState()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = new OrderRepository(context);

        var customerId = CustomerId.New();
        var order = Order.CreateDraft(customerId, Currency.USD, _now).Value;

        order.AddLine(
            ProductId.From(InMemoryProductCatalog.KeyboardId),
            "Ergonomic Mechanical Keyboard",
            Money.Create(149.99m, Currency.USD).Value,
            Quantity.Create(2).Value,
            _now);

        order.SetShippingAddress(
            Address.Create("500 Howard St", "San Francisco", "94105", "US").Value,
            _now);

        await repository.AddAsync(order);
        await context.SaveChangesAsync();

        // Clear tracking to force clean reload from SQLite
        context.ChangeTracker.Clear();

        var loaded = await repository.GetByIdAsync(order.Id);

        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(order.Id);
        loaded.CustomerId.ShouldBe(customerId);
        loaded.Currency.ShouldBe(Currency.USD);
        loaded.Status.ShouldBe(OrderStatus.Draft);
        loaded.Total.Amount.ShouldBe(299.98m);
        loaded.Total.Currency.ShouldBe(Currency.USD);
        loaded.Version.ShouldBe(3); // 1 (create) + 1 (add line) + 1 (set address)

        loaded.ShippingAddress.ShouldNotBeNull();
        loaded.ShippingAddress.Street.ShouldBe("500 Howard St");
        loaded.ShippingAddress.City.ShouldBe("San Francisco");
        loaded.ShippingAddress.PostalCode.ShouldBe("94105");
        loaded.ShippingAddress.CountryCode.ShouldBe("US");

        loaded.Lines.Count.ShouldBe(1);
        loaded.Lines[0].ProductId.Value.ShouldBe(InMemoryProductCatalog.KeyboardId);
        loaded.Lines[0].ProductName.ShouldBe("Ergonomic Mechanical Keyboard");
        loaded.Lines[0].UnitPrice.Amount.ShouldBe(149.99m);
        loaded.Lines[0].Quantity.Value.ShouldBe(2);
        loaded.Lines[0].LineTotal.Amount.ShouldBe(299.98m);
    }

    [Fact]
    public async Task UnitOfWork_WritesDomainChangesAndOutboxMessagesAtomically()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = new OrderRepository(context);

        var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _now).Value;
        order.AddLine(
            ProductId.From(InMemoryProductCatalog.KeyboardId),
            "Keyboard",
            Money.Create(100m, Currency.USD).Value,
            Quantity.Create(1).Value,
            _now);
        order.SetShippingAddress(Address.Create("123 St", "City", "12345", "US").Value, _now);

        // Submit raises OrderSubmittedDomainEvent which the interceptor dispatches to enqueue into Outbox
        order.Submit(_now);

        await repository.AddAsync(order);
        await context.SaveChangesAsync();

        // Verify outbox record committed in the same database transaction
        var outboxMessage = await context.OutboxMessages.FirstOrDefaultAsync(m => m.ContractName == "ordering.order-submitted.v1");
        outboxMessage.ShouldNotBeNull();
        outboxMessage.ProcessedOn.ShouldBeNull();
        outboxMessage.Payload.ShouldContain(order.Id.Value.ToString());
    }

    [Fact]
    public async Task OutboxProcessor_PublishesOnce_MarksProcessed_AndHaltsAfterMaxRetries()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var repository = new OrderRepository(context);

            var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _now).Value;
            order.AddLine(ProductId.From(InMemoryProductCatalog.KeyboardId), "Item", Money.Create(50m, Currency.USD).Value, Quantity.Create(1).Value, _now);
            order.SetShippingAddress(Address.Create("123 St", "City", "12345", "US").Value, _now);
            order.Submit(_now);

            await repository.AddAsync(order);
            await context.SaveChangesAsync();
        }

        var options = Options.Create(new OutboxOptions { MaxAttempts = 3, BatchSize = 10 });
        var processor = new OutboxProcessor(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            options,
            TimeProvider.System,
            NullLogger<OutboxProcessor>.Instance);

        // Drive first batch
        var processed = await processor.ProcessBatchAsync();
        processed.ShouldBe(1);

        _factory.PublishedEvents.Count.ShouldBe(1);
        _factory.PublishedEvents[0].ContractName.ShouldBe("ordering.order-submitted.v1");

        // Subsequent run should find 0 pending messages
        var secondRun = await processor.ProcessBatchAsync();
        secondRun.ShouldBe(0);
    }

    [Fact]
    public async Task SpecificationQueries_ExecuteDirectlyAgainstDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = new OrderRepository(context);

        var customerA = CustomerId.New();
        var customerB = CustomerId.New();

        var order1 = Order.CreateDraft(customerA, Currency.USD, _now).Value;
        var order2 = Order.CreateDraft(customerA, Currency.USD, _now).Value;
        var order3 = Order.CreateDraft(customerB, Currency.USD, _now).Value;

        await repository.AddAsync(order1);
        await repository.AddAsync(order2);
        await repository.AddAsync(order3);
        await context.SaveChangesAsync();

        var spec = new OrdersOfCustomerSpecification(customerA);
        var results = await repository.ListAsync(spec);

        results.Count.ShouldBe(2);
        results.ShouldAllBe(o => o.CustomerId == customerA);
    }

    [Fact]
    public async Task ConcurrencyConflict_ThrowsWhenTwoContextsUpdateSimultaneously()
    {
        var orderId = OrderId.New();

        using (var scopeInit = _factory.Services.CreateScope())
        {
            var initContext = scopeInit.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var order = Order.CreateDraft(CustomerId.New(), Currency.USD, _now).Value;
            typeof(Order).GetProperty("Id")!.SetValue(order, orderId);
            await initContext.Orders.AddAsync(order);
            await initContext.SaveChangesAsync();
        }

        // Two independent scopes load the same aggregate at Version 1
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();

        var context1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var context2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var orderFrom1 = await context1.Orders.FirstAsync(o => o.Id == orderId);
        var orderFrom2 = await context2.Orders.FirstAsync(o => o.Id == orderId);

        orderFrom1.AddLine(ProductId.New(), "Item 1", Money.Create(10m, Currency.USD).Value, Quantity.Create(1).Value, _now);
        orderFrom2.AddLine(ProductId.New(), "Item 2", Money.Create(20m, Currency.USD).Value, Quantity.Create(1).Value, _now);

        // First save succeeds and increments version to 2
        await context1.SaveChangesAsync();

        // Second save fails because version 1 in DB is now 2
        await Should.ThrowAsync<DddOrderAggregate.Application.Exceptions.ConcurrencyConflictException>(async () =>
        {
            await context2.SaveChangesAsync();
        });
    }
}
