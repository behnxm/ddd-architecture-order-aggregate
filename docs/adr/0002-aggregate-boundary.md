# 0002. Aggregate Boundary: Order Owns Lines, References External Aggregates by ID

## Context
A frequent pitfall in domain modeling is creating oversized aggregates that hold direct object references to external entities (e.g., `Order` holding a direct reference to a `Customer` or `Product` entity instance). This results in massive object graphs, severe lock contention, memory bloat, and ambiguous consistency boundaries. Conversely, anemic models sometimes expose child entities directly via their own repositories (e.g. creating an `IOrderLineRepository`).

## Decision
I established the aggregate consistency boundary strictly around `Order`:
1. **Order owns OrderLine**: `OrderLine` is an internal entity reachable exclusively through the `Order` root. It has no independent lifecycle, no public mutators, and no repository.
2. **References by ID Only**: External aggregates (`Customer` and `Product`) are referenced solely by their strongly-typed identifiers (`CustomerId` and `ProductId`).
3. **Product Snapshot**: When an order line is added, the aggregate captures an immutable snapshot of `ProductName` and `UnitPrice` rather than coupling to the live catalog entity.

## Consequences
- **Positive**: Modifying lines requires interacting through `Order` behaviour methods (`AddLine`, `ChangeLineQuantity`, `RemoveLine`), guaranteeing that aggregate invariants (max lines, line quantity range, duplicate product merging, and total recalculation) cannot be bypassed.
- **Positive**: The aggregate remains small, lightweight to hydrate from the database, and resilient against schema changes in upstream catalog or customer services.
- **Positive**: Historical purchase prices are legally fixed and immutable even if catalog prices change post-order creation.
