# Event Storming: Ordering Bounded Context

This event-storming record details the commands, triggers, resulting domain events, actors, and downstream side-effects modeled within the Ordering bounded context.

## Event Storming Model Table

| Command | Actor | Aggregate / Invariants | Domain Event Raised | Read Model / Policy Impact |
| :--- | :--- | :--- | :--- | :--- |
| **CreateDraftOrder** | Customer / Buyer | `Order.CreateDraft`<br/>- INV-01: Fixed Customer and Currency | *(None - Draft creation establishes aggregate boundary)* | Initializes `OrderSummary` read model with `Draft` status |
| **AddOrderLine** | Customer / Buyer | `Order.AddLine`<br/>- INV-02: Draft only<br/>- INV-03: Merge quantity if exists<br/>- INV-04: &le; 20 lines, 1..100 qty<br/>- INV-05: Positive unit price in order currency<br/>- INV-06: Recalculate order total | *(None - Local draft mutation)* | Updates line items, line totals, and aggregate total in read projection |
| **ChangeOrderLineQuantity** | Customer / Buyer | `Order.ChangeLineQuantity`<br/>- INV-02: Draft only<br/>- INV-04: 1..100 qty<br/>- INV-06: Recalculate order total | *(None - Local draft mutation)* | Updates line quantity and aggregate total in read projection |
| **RemoveOrderLine** | Customer / Buyer | `Order.RemoveLine`<br/>- INV-02: Draft only<br/>- INV-06: Recalculate order total | *(None - Local draft mutation)* | Removes line item and recalculates aggregate total in read projection |
| **SetShippingAddress** | Customer / Buyer | `Order.SetShippingAddress`<br/>- INV-02: Draft only<br/>- Valid address value object | *(None - Local draft mutation)* | Updates delivery address in read projection |
| **SubmitOrder** | Customer / Buyer | `Order.Submit`<br/>- INV-07: Shipping address required<br/>- INV-08: Non-empty order in Draft state<br/>- INV-09: Allowed state transition | `OrderSubmittedDomainEvent` | - Triggers Outbox enqueue for `OrderSubmittedIntegrationEvent`<br/>- Read model transitions to `Submitted`<br/>- Triggers downstream payment reservation policy |
| **MarkOrderPaid** | Payment Gateway / System | `Order.MarkPaid`<br/>- INV-09: Allowed from Submitted only | `OrderPaidDomainEvent` | - Read model transitions to `Paid`<br/>- Triggers warehouse picking and fulfillment policy |
| **MarkOrderShipped** | Warehouse / Dispatcher | `Order.MarkShipped`<br/>- INV-09: Allowed from Paid only | `OrderShippedDomainEvent` | - Read model transitions to `Shipped`<br/>- Notifies carrier tracking and fulfillment pipelines |
| **CancelOrder** | Customer / Customer Support | `Order.Cancel`<br/>- INV-09: Allowed from Draft, Submitted, or Paid; forbidden from Shipped or Cancelled<br/>- INV-10: Reason 1..200 chars required | `OrderCancelledDomainEvent` | - Triggers Outbox enqueue for `OrderCancelledIntegrationEvent`<br/>- Read model transitions to `Cancelled`<br/>- Triggers refund and inventory release policy |
| **CancelStaleDraftOrders** | System Scheduler / Maintenance Worker | `StaleDraftOrdersSpecification`<br/>- Identifies unsubmitted drafts older than threshold<br/>- Invokes `Order.Cancel` | `OrderCancelledDomainEvent` (per stale order) | Purges abandoned drafts from active checkout pipelines |
