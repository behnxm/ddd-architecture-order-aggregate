# 0007. Domain Events Dispatched Inside Transaction with Transactional Outbox for Integration Events

## Context
A major source of confusion in DDD is the conflation of **Domain Events** (in-memory notifications within the bounded context) and **Integration Events** (messages sent to external systems). If domain event handlers attempt to publish directly to external message brokers before the database transaction commits, a subsequent database rollback leaves the broker holding phantom messages (a dual-write problem).

## Decision
I separated in-memory domain events from external integration events using the Transactional Outbox pattern:
1. **Domain Events**: Aggregates raise domain events (`OrderSubmittedDomainEvent`, `OrderCancelledDomainEvent`, etc.) during valid state transitions.
2. **Pre-Commit Interceptor**: `DispatchDomainEventsInterceptor` intercepts `SavingChangesAsync`. Before the transaction commits, it collects uncommitted domain events, clears them from aggregates, and invokes `IDomainEventDispatcher`.
3. **Outbox Enqueuing**: Domain event handlers translate domain events into versioned `IIntegrationEvent` contracts and enqueue them via `IOutbox.Enqueue`. Because this occurs on the same ambient `ApplicationDbContext`, the domain aggregate mutations and the `outbox_messages` rows commit in the **exact same database transaction**.
4. **Asynchronous Outbox Processor**: A background worker (`OutboxProcessor`) periodically polls unprocessed outbox records, publishes them via `IIntegrationEventPublisher`, and marks them as processed with retry tracking.
5. **Selective Translation**: Not all domain events become integration events. For example, `OrderPaidDomainEvent` and `OrderShippedDomainEvent` execute structured audit logging only, demonstrating that domain events serve local decoupled concerns as well as integration triggers.

## Consequences
- **Positive**: Solves the dual-write problem completely: an integration event is persisted if and only if the aggregate state change commits.
- **Positive**: In-memory domain event failures abort the transaction cleanly, leaving no orphaned outbox rows.
- **Trade-off**: Requires an asynchronous background polling service and an outbox database table.
