using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;

namespace DddOrderAggregate.Application.Ports;

/// <summary>
/// Immutable snapshot of catalog product information translated across the anti-corruption layer.
/// </summary>
public sealed record ProductSnapshot(
    ProductId ProductId,
    string Name,
    Money UnitPrice);
