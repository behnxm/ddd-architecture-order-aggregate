using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Application.Ports;

/// <summary>
/// Anti-corruption port for reading product information from an upstream Product Catalog context.
/// Translates external schemas into a local ProductSnapshot.
/// </summary>
public interface IProductCatalog
{
    /// <summary>
    /// Retrieves a product snapshot by its catalog identifier.
    /// </summary>
    Task<Result<ProductSnapshot>> GetProductAsync(ProductId productId, CancellationToken cancellationToken = default);
}
