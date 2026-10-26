# 0008. Hand-Written Specification Pattern

## Context
Business logic regarding query criteria (e.g. what constitutes a "stale draft order" or which orders belong to a given customer) frequently gets duplicated between write aggregate methods, repository LINQ queries, and read projections. This creates semantic drift where different application layers define rules inconsistently.

## Decision
I implemented a hand-written `Specification<T>` base class exposing:
- `Expression<Func<T, bool>> Criteria { get; }`
- `bool IsSatisfiedBy(T entity)`: compiled once and cached using `Lazy<Func<T, bool>>`.
- Boolean composition operators: `And`, `Or`, and `Not` using a parameter-rebinding `ExpressionVisitor`.
- Alternative third-party libraries such as `Ardalis.Specification` (MIT License) were evaluated. While `Ardalis.Specification` is well-regarded, I implemented the specification pattern by hand in the Shared Kernel to maintain zero external dependencies in the Domain assembly and showcase the pure expression-tree visitor mechanics.

## Consequences
- **Positive**: Single Source of Truth: The criteria expression in `StaleDraftOrdersSpecification` is used identically by `IOrderRepository.ListAsync` (translated to SQL), `OrderReadRepository` (read projection), and `Order.IsStaleDraft` (in-memory evaluation).
- **Positive**: Zero external dependencies in the Domain project.
- **Positive**: Clean composability allowing dynamic chaining of business predicates.
