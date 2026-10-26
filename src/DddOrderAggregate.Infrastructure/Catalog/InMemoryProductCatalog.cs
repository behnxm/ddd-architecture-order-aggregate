using System.Collections.Frozen;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders.Errors;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Infrastructure.Catalog;

/// <summary>
/// Anti-Corruption Layer (ACL) adapter simulating integration with an external Product Catalog bounded context.
/// 
/// In enterprise systems, this adapter would call the Catalog context's REST API or gRPC service.
/// Here, it encapsulates the upstream model and maps it cleanly into Ordering's local ProductSnapshot contract.
/// Seeded with 5 fixed GUID products (4 in USD, 1 in EUR) to facilitate interactive testing via requests/orders.http.
/// </summary>
public sealed class InMemoryProductCatalog : IProductCatalog
{
    public static readonly Guid KeyboardId = Guid.Parse("01950000-0000-7000-8000-000000000001");
    public static readonly Guid MouseId = Guid.Parse("01950000-0000-7000-8000-000000000002");
    public static readonly Guid MonitorId = Guid.Parse("01950000-0000-7000-8000-000000000003");
    public static readonly Guid HeadphonesId = Guid.Parse("01950000-0000-7000-8000-000000000004");
    public static readonly Guid EuroAdapterId = Guid.Parse("01950000-0000-7000-8000-000000000005");

    private static readonly FrozenDictionary<Guid, ProductSnapshot> SeededProducts =
        new Dictionary<Guid, ProductSnapshot>
        {
            [KeyboardId] = new ProductSnapshot(
                ProductId.From(KeyboardId),
                "Ergonomic Mechanical Keyboard",
                Money.Create(149.99m, Currency.USD).Value),

            [MouseId] = new ProductSnapshot(
                ProductId.From(MouseId),
                "Wireless Precision Mouse",
                Money.Create(79.50m, Currency.USD).Value),

            [MonitorId] = new ProductSnapshot(
                ProductId.From(MonitorId),
                "4K Ultra-Wide Monitor",
                Money.Create(499.00m, Currency.USD).Value),

            [HeadphonesId] = new ProductSnapshot(
                ProductId.From(HeadphonesId),
                "Noise-Cancelling Headphones",
                Money.Create(199.95m, Currency.USD).Value),

            // Seeded in EUR specifically to demonstrate cross-currency invariant violations against USD orders
            [EuroAdapterId] = new ProductSnapshot(
                ProductId.From(EuroAdapterId),
                "Euro USB-C Power Adapter",
                Money.Create(29.99m, Currency.EUR).Value)
        }.ToFrozenDictionary();

    public Task<Result<ProductSnapshot>> GetProductAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        if (!SeededProducts.TryGetValue(productId.Value, out var product))
        {
            return Task.FromResult<Result<ProductSnapshot>>(
                Error.NotFound("Catalog.ProductNotFound", $"Product '{productId.Value}' was not found in the catalog."));
        }

        return Task.FromResult(Result.Success(product));
    }
}
