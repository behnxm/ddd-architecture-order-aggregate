# 0010. Aggregate-Owned Optimistic Concurrency with HTTP ETag and If-Match

## Context
Concurrent mutations on an order aggregate (e.g. two browser tabs submitting line additions simultaneously, or customer support cancelling while the buyer submits) risk lost updates. Standard approaches often push concurrency tokens entirely to database-managed row version timestamps, bypassing domain awareness.

## Decision
I designed a two-tiered optimistic concurrency strategy owned directly by the domain aggregate:
1. **Domain-Owned Concurrency Token**: `AggregateRoot.Version` is an integer token that starts at `1` upon creation and is explicitly incremented by `1` within the aggregate root via `Touch(DateTimeOffset now)` upon every successful mutation. If a mutation fails invariant validation, the version remains unchanged.
2. **EF Core Concurrency Mapping**: EF Core maps `Version` as an explicit concurrency token (`builder.Property(o => o.Version).IsConcurrencyToken()`). On `SaveChangesAsync`, EF Core adds `WHERE Version = @p0` to UPDATE statements, throwing `DbUpdateConcurrencyException` if another transaction modified the row concurrently.
3. **HTTP ETag & If-Match Protocol**:
   - Every read and mutation response outputs an HTTP `ETag` header containing the aggregate version: `ETag: "1"`.
   - Mutating API endpoints accept an optional `If-Match: "1"` header.
   - If `If-Match` is provided and does not match `order.Version`, the API returns HTTP 412 Precondition Failed before executing any mutations.
   - If two requests pass the version check concurrently, the EF Core concurrency token acts as the definitive transactional backstop, caught by `GlobalExceptionHandler` and returned as HTTP 409 Conflict.

## Consequences
- **Positive**: Complete protection against lost updates across concurrent requests.
- **Positive**: Strict adherence to RFC 9110 HTTP semantics (412 for precondition failure, 409 for transactional concurrency collision).
- **Positive**: The domain aggregate maintains explicit control over its own mutation lifecycle.
