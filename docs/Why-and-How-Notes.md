# Tactical Domain-Driven Design: Interview Questions & Deep Dive

This document contains 15 technical questions and detailed architectural answers based on this repository's implementation of tactical Domain-Driven Design in .NET 10. Each answer explains the design rationale, trade-offs, and points to concrete source files.

---

### 1. How do you define aggregate boundaries, and why does `OrderLine` have no repository?
**Answer**:
An Aggregate defines a transactional consistency boundary. In DDD, external consumers are permitted to hold references only to the Aggregate Root (`Order`), never directly to internal child entities. 

In this codebase ([`Order.cs`](../src/DddOrderAggregate.Domain/Orders/Order.cs#L14-L40)), `OrderLine` ([`OrderLine.cs`](../src/DddOrderAggregate.Domain/Orders/OrderLine.cs#L12-L30)) is an internal entity that cannot exist without its parent `Order`. Invariants such as maximum lines per order (INV-04), merging duplicate product quantities (INV-03), and recalculating the total amount (INV-06) can only be protected if all modifications pass through the root's methods. If `OrderLine` had its own repository (`IOrderLineRepository`), clients could bypass the root, mutating line quantities directly in the database and leaving the aggregate total and invariant rules corrupted. Consequently, [`IOrderRepository.cs`](../src/DddOrderAggregate.Domain/Orders/IOrderRepository.cs) is the sole repository, always hydrating the whole aggregate with its lines.

---

### 2. What is the fundamental difference between an Entity and a Value Object?
**Answer**:
- **Entity** ([`Entity.cs`](../src/DddOrderAggregate.Domain/SharedKernel/Entity.cs)): An entity is defined by its continuous identity across time rather than its attributes. Two `Order` or `OrderLine` instances with different attributes are equal if their IDs match.
- **Value Object** ([`Money.cs`](../src/DddOrderAggregate.Domain/Orders/ValueObjects/Money.cs), [`Address.cs`](../src/DddOrderAggregate.Domain/Orders/ValueObjects/Address.cs), [`Quantity.cs`](../src/DddOrderAggregate.Domain/Orders/ValueObjects/Quantity.cs)): A value object is descriptive, immutable, has no conceptual identity, and two instances are equal if all their attributes are equal. In this codebase, value objects are implemented as C# `sealed record` or `readonly record struct` types with private constructors and static `Result`-returning factories. Their copy constructors are private to prevent bypassing domain invariant validation via C# `with` expressions.

---

### 3. Why did you use the Result pattern instead of throwing domain exceptions for business validation?
**Answer**:
Exceptions should be reserved for truly exceptional situations (programmer bugs, corrupted database state, hardware failure) rather than predictable domain outcomes.

In this codebase ([`Result.cs`](../src/DddOrderAggregate.Domain/SharedKernel/Results/Result.cs), [`OrderErrors.cs`](../src/DddOrderAggregate.Domain/Orders/Errors/OrderErrors.cs)):
- Operations like attempting to add a line to a submitted order (`OrderErrors.NotDraft`) or passing an invalid quantity are expected business scenarios.
- Returning `Result` or `Result<T>` documents failure paths directly in the C# method signature, making flow control transparent.
- It eliminates the significant performance cost of unwinding the stack on expected validation failures.
- In contrast, runtime guard clauses (`ArgumentNullException.ThrowIfNull`) that signal developer contract violations throw standard exceptions.

---

### 4. What is the difference between Domain Events and Integration Events?
**Answer**:
- **Domain Events** ([`IDomainEvent.cs`](../src/DddOrderAggregate.Domain/SharedKernel/IDomainEvent.cs), [`OrderSubmittedDomainEvent.cs`](../src/DddOrderAggregate.Domain/Orders/Events/OrderSubmittedDomainEvent.cs)): In-memory domain occurrences raised within the aggregate boundary. They represent state transitions inside the bounded context and are dispatched synchronously before the database transaction commits. They can reference domain entities and strongly-typed IDs.
- **Integration Events** ([`IIntegrationEvent.cs`](../src/DddOrderAggregate.Application/IntegrationEvents/IIntegrationEvent.cs), [`OrderSubmittedIntegrationEvent.cs`](../src/DddOrderAggregate.Application/IntegrationEvents/OrderSubmittedIntegrationEvent.cs)): Public asynchronous contracts published across bounded context boundaries. They use stable, schema-versioned contracts (e.g. `ordering.order-submitted.v1`) and primitive types (`Guid`, `decimal`, `string`), strictly shielding external systems from internal domain changes.
- In [`OrderSubmittedDomainEventHandler.cs`](../src/DddOrderAggregate.Application/Orders/EventHandlers/OrderSubmittedDomainEventHandler.cs), the domain event is translated into an integration event and enqueued in the Outbox. Meanwhile, [`OrderPaidDomainEventHandler.cs`](../src/DddOrderAggregate.Application/Orders/EventHandlers/OrderPaidDomainEventHandler.cs) demonstrates that not all domain events become integration events.

---

### 5. Why is the Transactional Outbox pattern necessary, and how does your interceptor participate?
**Answer**:
Directly publishing to an external message broker inside an application handler introduces the **dual-write problem**: if the broker publish succeeds but the subsequent database commit fails, the broker emits a phantom message. If the database commits first and the broker is down, the message is permanently lost.

In this architecture:
1. When the aggregate mutates, domain events are staged in-memory.
2. [`DispatchDomainEventsInterceptor.cs`](../src/DddOrderAggregate.Infrastructure/Interceptors/DispatchDomainEventsInterceptor.cs) intercepts `SavingChangesAsync`.
3. The interceptor dispatches domain events to handlers, which call `IOutbox.Enqueue`, adding rows to `outbox_messages` on the **same** `ApplicationDbContext`.
4. The aggregate state and the outbox records commit in **one atomic database transaction**.
5. [`OutboxProcessor.cs`](../src/DddOrderAggregate.Infrastructure/Outbox/OutboxProcessor.cs) asynchronously polls unprocessed rows, publishes them via `IIntegrationEventPublisher`, and marks them processed.

---

### 6. How does optimistic concurrency work with aggregate versioning and HTTP ETags?
**Answer**:
Optimistic concurrency is owned directly by the domain aggregate:
1. `AggregateRoot<TId>.Version` ([`AggregateRoot.cs`](../src/DddOrderAggregate.Domain/SharedKernel/AggregateRoot.cs)) starts at `1` and increments on every successful state change via `Touch(now)`.
2. EF Core maps `Version` as an explicit concurrency token ([`OrderConfiguration.cs`](../src/DddOrderAggregate.Infrastructure/Persistence/Configurations/OrderConfiguration.cs)).
3. HTTP GET and mutation responses output `ETag: "{version}"` ([`OrderEndpoints.cs`](../src/DddOrderAggregate.Api/Endpoints/OrderEndpoints.cs)).
4. Mutating endpoints accept `If-Match: "{version}"`. If the client passes a stale version, the endpoint immediately returns HTTP 412 Precondition Failed.
5. If two requests pass the version check simultaneously, EF Core's concurrency token detects the collision during `SaveChangesAsync`, throwing `ConcurrencyConflictException` which [`GlobalExceptionHandler.cs`](../src/DddOrderAggregate.Api/Common/GlobalExceptionHandler.cs) maps to HTTP 409 Conflict.

---

### 7. Why use strongly-typed IDs over plain Guids or longs?
**Answer**:
Primitive obsession with plain `Guid` or `long` parameters leads to silent transposition bugs. A method accepting `void AssignOrder(Guid orderId, Guid customerId, Guid productId)` allows callers to pass IDs in the wrong order without any compiler error.

By implementing `OrderId`, `CustomerId`, `ProductId`, and `OrderLineId` as `readonly record struct` types ([`OrderId.cs`](../src/DddOrderAggregate.Domain/Orders/Identifiers/OrderId.cs)), the compiler enforces type safety. Furthermore:
- They implement `IStronglyTypedId<TSelf>` using C# 14 static abstract interface methods (`From(Guid)`).
- Identity generation uses UUIDv7 (`Guid.CreateVersion7()`), which combines a Unix millisecond timestamp with random bits, ensuring chronological sorting and optimal B-tree index insertion performance.
- They are converted automatically via [`StronglyTypedIdConverter.cs`](../src/DddOrderAggregate.Infrastructure/Persistence/Converters/StronglyTypedIdConverter.cs) in EF Core conventions.

---

### 8. Why capture a snapshot of product details rather than referencing the Product entity?
**Answer**:
In e-commerce, an order line represents an agreed legal contract at a specific point in time. 
- If `OrderLine` held a live foreign key reference to `Product.Price`, then whenever product catalog administrators updated the price or renamed the product, pending and historical orders would dynamically reflect the new price.
- In [`OrderLine.cs`](../src/DddOrderAggregate.Domain/Orders/OrderLine.cs), `ProductName` and `UnitPrice` are stored as an immutable snapshot captured at the instant the line is added. Even if catalog prices double tomorrow, the customer's draft and submitted order preserves the agreed snapshot price.

---

### 9. How does the Specification pattern guarantee a single source of truth across write and read models?
**Answer**:
Business rules often drift when query predicates are duplicated across different layers. For example, what constitutes a "stale draft order" might be written as `order.Status == Draft && order.UpdatedAt < cutoff` in a background service, but written slightly differently in an aggregate query method or read repository.

In [`StaleDraftOrdersSpecification.cs`](../src/DddOrderAggregate.Domain/Orders/Specifications/StaleDraftOrdersSpecification.cs):
- The domain rule is defined once in an `Expression<Func<Order, bool>> Criteria` property.
- The write repository ([`OrderRepository.cs`](../src/DddOrderAggregate.Infrastructure/Repositories/OrderRepository.cs)) applies the expression directly to EF Core, translating it to SQL.
- The read repository ([`OrderReadRepository.cs`](../src/DddOrderAggregate.Infrastructure/ReadModels/OrderReadRepository.cs)) reuses the exact same specification for filtering.
- In-memory evaluations ([`Order.IsStaleDraft`](../src/DddOrderAggregate.Domain/Orders/Order.cs)) evaluate the compiled expression via `IsSatisfiedBy`.

---

### 10. Why inject use case handlers directly instead of using a mediator library?
**Answer**:
In-process mediator libraries (such as MediatR) are frequently used uncritically under the guise of CQRS. However:
1. In-process mediation relies on runtime reflection to dispatch requests.
2. It breaks static code analysis and IDE navigation ("Find References" or "Go to Implementation" on an endpoint takes you to an empty request marker interface rather than the handler code).
3. It encourages anemic handlers that simply forward requests.
4. Licensing changes in MediatR v13+ introduce proprietary risks.

In this codebase ([`DependencyInjection.cs`](../src/DddOrderAggregate.Application/DependencyInjection.cs)):
- Every use case is an explicit, sealed handler class (e.g. [`SubmitOrderCommandHandler.cs`](../src/DddOrderAggregate.Application/Orders/Commands/SubmitOrderCommandHandler.cs)).
- Handlers are scanned and registered in DI at startup and injected directly into Minimal API endpoints via `[FromServices]`.
- Execution is direct, type-safe, and instantly navigable in any IDE with zero reflection overhead.

---

### 11. How would you transition this single-instance outbox processor to run across multiple worker nodes safely?
**Answer**:
In this reference implementation ([`OutboxProcessor.cs`](../src/DddOrderAggregate.Infrastructure/Outbox/OutboxProcessor.cs)), the background processor assumes a single running application instance against SQLite.

To scale out across multiple horizontal instances:
1. **PostgreSQL / SQL Server Row Locking**: In PostgreSQL, workers would query using `FOR UPDATE SKIP LOCKED`:
   ```sql
   SELECT * FROM outbox_messages 
   WHERE processed_on IS NULL AND attempts < 5 
   ORDER BY occurred_on 
   LIMIT 20 
   FOR UPDATE SKIP LOCKED;
   ```
   This ensures each node acquires exclusive locks on distinct rows without blocking concurrent worker threads.
2. **Distributed Leases**: Alternatively, an orchestration tool like Hangfire or Quartz with distributed database locks can coordinate job instances.
3. **Consumer Idempotency**: Because outbox delivery is at-least-once, downstream consumers must deduplicate messages using `MessageId` (UUIDv7) stored in an inbox table.

---

### 12. Why did you choose EF Core 10 Complex Properties for Money and Address, but OwnsMany for Lines?
**Answer**:
- **Complex Properties** ([`OrderConfiguration.cs`](../src/DddOrderAggregate.Infrastructure/Persistence/Configurations/OrderConfiguration.cs)): In EF Core 8/9/10, complex types represent true value objects. They are not entities, have no identity keys (not even shadow keys), and their properties are flattened directly into the table. `Order.Total` and `Order.ShippingAddress` are modeled as complex properties; `ShippingAddress` is optional, which EF Core 10 supports natively on complex types.
- **Owned Collections (`OwnsMany`)**: Value objects in DDD cannot exist in multiple instances without identity if they need to be queried or updated as relational collections. `Order.Lines` is a collection of internal entities. `OwnsMany` flattens the collection into an `order_lines` table with a foreign key shadow property linking to `OrderId`, and `AutoInclude()` ensures the aggregate root is always loaded whole.
- **Owned Entity Child Mapping**: In EF Core 10, `OwnedNavigationBuilder` does not support `.ComplexProperty()` on owned child entities. Therefore, `OrderLine.UnitPrice` is mapped via a nested `OwnsOne(l => l.UnitPrice)`.

---

### 13. How does the BannedApiAnalyzer safeguard domain determinism?
**Answer**:
Aggregates must be deterministic state machines. If domain logic invokes `DateTime.UtcNow` or `Guid.NewGuid()`, unit tests become non-deterministic, time travel is impossible, and mocking the system clock pollutes domain signatures.

In [`BannedSymbols.txt`](../src/DddOrderAggregate.Domain/BannedSymbols.txt):
- `Microsoft.CodeAnalysis.BannedApiAnalyzers` runs as a compile-time analyzer (`PrivateAssets=all`).
- Any usage of `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`, or `Guid.NewGuid()` in the Domain fails compilation with an error.
- All aggregate methods receive `DateTimeOffset now` as an explicit parameter.
- The Application layer uses `TimeProvider`, which allows unit tests to inject `FakeTimeProvider` for complete control over time progression.

---

### 14. How would you split Ordering and Catalog into truly separate microservices over the network?
**Answer**:
The architecture is already pre-factored for physical microservice separation:
1. In [`context-map.md`](domain/context-map.md), the relationship between Ordering and Catalog is defined as Customer/Supplier with an Anti-Corruption Layer (ACL).
2. The Application layer depends only on the [`IProductCatalog`](../src/DddOrderAggregate.Application/Ports/IProductCatalog.cs) port, which returns a local `ProductSnapshot`.
3. To split Catalog into a remote service:
   - Replace [`InMemoryProductCatalog.cs`](../src/DddOrderAggregate.Infrastructure/Catalog/InMemoryProductCatalog.cs) with an `HttpProductCatalog` or `GrpcProductCatalog` that queries Catalog over HTTP/gRPC with Polly resiliency (retries, circuit breakers).
   - Alternatively, subscribe to `catalog.product-price-changed.v1` integration events and store local product pricing read caches.
   - The Domain and Application layers remain 100% untouched because the ACL port absorbs all network protocols.

---

### 15. How would you incorporate a payment gateway into this aggregate lifecycle without violating bounded context purity?
**Answer**:
Directly invoking an external payment gateway (e.g. Stripe API) inside `Order.Submit()` or inside an entity method would severely violate DDD principles by mixing remote I/O and non-deterministic third-party SDKs into the domain.

The correct architectural integration:
1. `Order.Submit(now)` transitions the order to `Submitted`, raises `OrderSubmittedDomainEvent`, and enqueues `OrderSubmittedIntegrationEvent`.
2. A separate Payment Bounded Context (or application worker) listens to `OrderSubmittedIntegrationEvent`.
3. The Payment service handles customer cards, webhooks, 3D Secure redirects, and retries.
4. When payment succeeds, the Payment context publishes `payment.settled.v1`.
5. An integration consumer in Ordering receives `payment.settled.v1`, loads the aggregate via `IOrderRepository`, and calls [`Order.MarkPaid(now)`](../src/DddOrderAggregate.Domain/Orders/Order.cs), transitioning the order to `Paid` status cleanly within its own bounded context.
