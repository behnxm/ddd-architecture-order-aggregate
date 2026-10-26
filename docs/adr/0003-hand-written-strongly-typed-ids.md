# 0003. Hand-Written Strongly-Typed Identifiers

## Context
Primitive obsession with `Guid` identifiers exposes enterprise applications to silent argument transposition bugs (such as passing a `CustomerId` into an API method expecting an `OrderId`). While source generators exist, adding third-party source-generator dependencies to the core Domain layer introduces compile-time framework coupling and external licensing risks.

## Decision
I implemented strongly-typed identifiers as hand-written `readonly record struct` types implementing a common `IStronglyTypedId<TSelf>` contract with static abstract factory methods:
- `OrderId`, `OrderLineId`, `ProductId`, `CustomerId`.
- Each identifier generates sequential UUIDv7 values via `Guid.CreateVersion7()`, rejects empty GUIDs (`Guid.Empty`), and formats cleanly as strings.
- Alternative source-generator packages were evaluated, including `StronglyTypedId` by Andrew Lock (MIT License) and `Vogen` by Steve Collins (MIT License). While both are excellent, I rejected third-party generators in favour of pure C# 14 zero-dependency code to demonstrate that modern C# features allow idiomatic, low-ceremony strongly-typed IDs without runtime or compile-time dependencies.

## Consequences
- **Positive**: Zero external dependencies in the Domain assembly.
- **Positive**: Compile-time type safety preventing ID transposition errors.
- **Positive**: Value type semantics (`readonly record struct`) ensuring zero heap allocations during identity comparisons.
- **Positive**: Sequential UUIDv7 ensures optimal database B-tree index clustering in relational tables.
- **Trade-off**: Requires writing ~25 lines of struct boilerplate per distinct identifier type.
