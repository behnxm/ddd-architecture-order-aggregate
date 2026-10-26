# Ubiquitous Language

This glossary establishes the unambiguous shared vocabulary for the Ordering bounded context. Every concept, state, and operation defined here maps directly to concrete classes, value objects, and domain methods in the codebase.

| Term | Ubiquitous Definition | Code Representation |
| :--- | :--- | :--- |
| **Order** | The aggregate root representing a commercial commitment between a specific customer and the merchant. | `DddOrderAggregate.Domain.Orders.Order` |
| **Draft** | The initial mutable lifecycle state of an order where lines and shipping address can be modified. | `OrderStatus.Draft` |
| **Submitted** | The immutable state reached once a draft order is confirmed by the customer, locking all lines and pricing for fulfillment. | `OrderStatus.Submitted` |
| **Paid** | The state indicating financial settlement has been successfully confirmed for the submitted order. | `OrderStatus.Paid` |
| **Shipped** | The terminal fulfilled state indicating goods have been dispatched to the shipping address. | `OrderStatus.Shipped` |
| **Cancelled** | The terminal aborted state indicating the order was terminated prior to fulfillment with a recorded reason. | `OrderStatus.Cancelled` |
| **Order Line** | An internal entity of the Order aggregate representing an item quantity snapshot bound to a specific catalog product. | `DddOrderAggregate.Domain.Orders.OrderLine` |
| **Unit Price** | The non-negative monetary price per single unit of a product snapshot captured at the moment of adding to an order. | `OrderLine.UnitPrice` (`Money`) |
| **Line Total** | The computed monetary product of an order line's quantity multiplied by its snapshot unit price. | `OrderLine.LineTotal` (`Money`) |
| **Quantity** | An integer value object constrained strictly between 1 and 100 representing item counts. | `DddOrderAggregate.Domain.Orders.ValueObjects.Quantity` |
| **Money** | An immutable monetary value object containing a non-negative decimal amount with at most two decimal places and a specific currency. | `DddOrderAggregate.Domain.Orders.ValueObjects.Money` |
| **Shipping Address** | An immutable physical delivery destination value object containing validated street, city, postal code, and country code. | `DddOrderAggregate.Domain.Orders.ValueObjects.Address` |
| **Submit** | The business operation transitioning an order from Draft to Submitted, requiring an address and at least one line. | `Order.Submit(DateTimeOffset now)` |
| **Pay** | The business operation transitioning a Submitted order to Paid upon verified settlement. | `Order.MarkPaid(DateTimeOffset now)` |
| **Ship** | The business operation transitioning a Paid order to Shipped upon carrier dispatch. | `Order.MarkShipped(DateTimeOffset now)` |
| **Cancel** | The business operation transitioning a Draft, Submitted, or Paid order to Cancelled with a mandatory reason. | `Order.Cancel(string reason, DateTimeOffset now)` |
| **Stale Draft** | An unsubmitted draft order whose last update timestamp precedes an established retention cutoff. | `StaleDraftOrdersSpecification` / `Order.IsStaleDraft(DateTimeOffset cutoff)` |
