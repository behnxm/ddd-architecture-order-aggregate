# 0001. Tactical DDD Scope and Single Bounded Context

## Context
When demonstrating tactical Domain-Driven Design (DDD) in .NET, architectures frequently suffer from two opposing failure modes: either they become toy CRUD implementations that fail to showcase actual aggregate boundaries, or they introduce excessive distributed systems complexity (such as distributed sagas, multi-tenancy, and event sourcing) that obscures the core DDD patterns.

## Decision
I scoped this repository strictly to a single bounded context: **Ordering**. Inside this context, I modeled exactly one aggregate root: `Order`.
- Other upstream domains (Product Catalog and Customer Identity) are referenced by external identifier or consumed via an anti-corruption adapter.
- Advanced distributed concerns (such as event sourcing, distributed 2PC transactions, or multi-tenant database partitioning) were deliberately excluded to focus exclusively on pristine tactical modeling: rich aggregate boundaries, invariant protection, value objects, domain events, specifications, and the transactional outbox.

## Consequences
- **Positive**: Every core tactical DDD concept appears with absolute clarity and maximum pedagogical value without architectural noise.
- **Positive**: The codebase serves as an interview-ready reference for pure aggregate modeling and EF Core persistence mapping.
- **Trade-off**: Multi-aggregate transactional coordination across independent boundaries is not modeled; in production, cross-aggregate workflows would be coordinated via integration events or orchestration workers.
