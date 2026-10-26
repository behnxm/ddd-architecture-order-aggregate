using System.Collections.Frozen;
using DddOrderAggregate.Domain.Orders.Errors;
using DddOrderAggregate.Domain.Orders.Events;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.Specifications;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Domain.Orders;

/// <summary>
/// Aggregate Root representing an Order commercial commitment.
/// Protects all invariants across its enclosed lines and manages lifecycle state transitions.
/// </summary>
public sealed class Order : AggregateRoot<OrderId>
{
    public const int MaxLines = 20;
    public const int MaxCancellationReasonLength = 200;

    private static readonly FrozenDictionary<OrderStatus, IReadOnlySet<OrderStatus>> AllowedTransitions =
        new Dictionary<OrderStatus, IReadOnlySet<OrderStatus>>
        {
            [OrderStatus.Draft] = new HashSet<OrderStatus> { OrderStatus.Submitted, OrderStatus.Cancelled },
            [OrderStatus.Submitted] = new HashSet<OrderStatus> { OrderStatus.Paid, OrderStatus.Cancelled },
            [OrderStatus.Paid] = new HashSet<OrderStatus> { OrderStatus.Shipped, OrderStatus.Cancelled },
            [OrderStatus.Shipped] = new HashSet<OrderStatus>(),
            [OrderStatus.Cancelled] = new HashSet<OrderStatus>()
        }.ToFrozenDictionary();

    private readonly List<OrderLine> _lines = [];

    public CustomerId CustomerId { get; private set; }
    public Currency Currency { get; private set; }
    public OrderStatus Status { get; private set; }
    public Address? ShippingAddress { get; private set; }
    public Money Total { get; private set; } = default!;

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// Parameterless constructor required for EF Core materialization.
    /// </summary>
    private Order()
    {
    }

    private Order(
        OrderId id,
        CustomerId customerId,
        Currency currency,
        DateTimeOffset createdAt) : base(id, createdAt)
    {
        CustomerId = customerId;
        Currency = currency;
        Status = OrderStatus.Draft;
        Total = Money.Zero(currency);
    }

    /// <summary>
    /// Creates a new Order in Draft status for a given customer and currency.
    /// INV-01: Customer and currency are fixed at creation.
    /// </summary>
    public static Result<Order> CreateDraft(CustomerId customerId, Currency currency, DateTimeOffset now)
    {
        var order = new Order(OrderId.New(), customerId, currency, now);
        return Result.Success(order);
    }

    /// <summary>
    /// Checks whether the aggregate can transition from its current status to the requested target.
    /// </summary>
    public bool CanTransitionTo(OrderStatus target) =>
        AllowedTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(target);

    /// <summary>
    /// Adds a product line item to the draft order.
    /// INV-02: Allowed only in Draft status.
    /// INV-03: Merges quantity if product already exists, keeping original unit price snapshot.
    /// INV-04: At most 20 distinct lines per order; quantity 1..100.
    /// INV-05: Unit price must be positive and match the order currency.
    /// INV-06: Automatically recalculates order total.
    /// </summary>
    public Result AddLine(ProductId productId, string productName, Money unitPrice, Quantity quantity, DateTimeOffset now)
    {
        if (Status != OrderStatus.Draft)
        {
            return OrderErrors.NotDraft(Status);
        }

        if (unitPrice.Currency != Currency)
        {
            return OrderErrors.CurrencyMismatch(Currency.Code, unitPrice.Currency.Code);
        }

        if (unitPrice.Amount <= 0m)
        {
            return OrderErrors.InvalidAmount(unitPrice.Amount);
        }

        var existing = _lines.FirstOrDefault(l => l.ProductId == productId);
        if (existing is not null)
        {
            var mergeResult = existing.MergeQuantity(quantity);
            if (mergeResult.IsFailure)
            {
                return mergeResult;
            }

            RecalculateTotal();
            Touch(now);
            return Result.Success();
        }

        if (_lines.Count >= MaxLines)
        {
            return OrderErrors.TooManyLines(MaxLines);
        }

        var lineResult = OrderLine.Create(productId, productName, unitPrice, quantity);
        if (lineResult.IsFailure)
        {
            return lineResult;
        }

        _lines.Add(lineResult.Value);
        RecalculateTotal();
        Touch(now);
        return Result.Success();
    }

    /// <summary>
    /// Updates the quantity of an existing line item.
    /// INV-02: Allowed only in Draft status.
    /// INV-06: Automatically recalculates order total.
    /// </summary>
    public Result ChangeLineQuantity(OrderLineId lineId, Quantity quantity, DateTimeOffset now)
    {
        if (Status != OrderStatus.Draft)
        {
            return OrderErrors.NotDraft(Status);
        }

        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null)
        {
            return OrderErrors.LineNotFound(lineId);
        }

        line.ChangeQuantity(quantity);
        RecalculateTotal();
        Touch(now);
        return Result.Success();
    }

    /// <summary>
    /// Removes an existing line item from the draft order.
    /// INV-02: Allowed only in Draft status.
    /// INV-06: Automatically recalculates order total.
    /// </summary>
    public Result RemoveLine(OrderLineId lineId, DateTimeOffset now)
    {
        if (Status != OrderStatus.Draft)
        {
            return OrderErrors.NotDraft(Status);
        }

        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null)
        {
            return OrderErrors.LineNotFound(lineId);
        }

        _lines.Remove(line);
        RecalculateTotal();
        Touch(now);
        return Result.Success();
    }

    /// <summary>
    /// Sets or updates the shipping address for delivery.
    /// INV-02: Allowed only in Draft status.
    /// </summary>
    public Result SetShippingAddress(Address address, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (Status != OrderStatus.Draft)
        {
            return OrderErrors.NotDraft(Status);
        }

        ShippingAddress = address;
        Touch(now);
        return Result.Success();
    }

    /// <summary>
    /// Submits the draft order for processing and fulfillment.
    /// INV-07: Shipping address is required.
    /// INV-08: Order must not be empty.
    /// INV-09: Transition allowed only from Draft.
    /// INV-12: Domain event is raised on success.
    /// </summary>
    public Result Submit(DateTimeOffset now)
    {
        if (!CanTransitionTo(OrderStatus.Submitted))
        {
            return OrderErrors.InvalidTransition(Status, OrderStatus.Submitted);
        }

        if (_lines.Count == 0)
        {
            return OrderErrors.Empty;
        }

        if (ShippingAddress is null)
        {
            return OrderErrors.AddressRequired;
        }

        Status = OrderStatus.Submitted;
        SubmittedAt = now;
        Touch(now);
        RaiseDomainEvent(OrderSubmittedDomainEvent.Create(Id, CustomerId, Total, now));
        return Result.Success();
    }

    /// <summary>
    /// Records payment confirmation for the submitted order.
    /// INV-09: Transition allowed only from Submitted.
    /// INV-12: Domain event is raised on success.
    /// </summary>
    public Result MarkPaid(DateTimeOffset now)
    {
        if (!CanTransitionTo(OrderStatus.Paid))
        {
            return OrderErrors.InvalidTransition(Status, OrderStatus.Paid);
        }

        Status = OrderStatus.Paid;
        PaidAt = now;
        Touch(now);
        RaiseDomainEvent(OrderPaidDomainEvent.Create(Id, now));
        return Result.Success();
    }

    /// <summary>
    /// Marks the paid order as dispatched from warehouse.
    /// INV-09: Transition allowed only from Paid.
    /// INV-12: Domain event is raised on success.
    /// </summary>
    public Result MarkShipped(DateTimeOffset now)
    {
        if (!CanTransitionTo(OrderStatus.Shipped))
        {
            return OrderErrors.InvalidTransition(Status, OrderStatus.Shipped);
        }

        Status = OrderStatus.Shipped;
        ShippedAt = now;
        Touch(now);
        RaiseDomainEvent(OrderShippedDomainEvent.Create(Id, now));
        return Result.Success();
    }

    /// <summary>
    /// Cancels the order before fulfillment with a mandatory reason.
    /// INV-09: Allowed from Draft, Submitted, or Paid; forbidden from Shipped or Cancelled.
    /// INV-10: Reason must be between 1 and 200 characters.
    /// INV-12: Domain event is raised on success.
    /// </summary>
    public Result Cancel(string reason, DateTimeOffset now)
    {
        if (!CanTransitionTo(OrderStatus.Cancelled))
        {
            return OrderErrors.InvalidTransition(Status, OrderStatus.Cancelled);
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 1 or > MaxCancellationReasonLength)
        {
            return OrderErrors.InvalidCancellationReason;
        }

        Status = OrderStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAt = now;
        Touch(now);
        RaiseDomainEvent(OrderCancelledDomainEvent.Create(Id, CancellationReason, now));
        return Result.Success();
    }

    /// <summary>
    /// Evaluates if this draft order is stale according to the single-source-of-truth specification.
    /// </summary>
    public bool IsStaleDraft(DateTimeOffset cutoff) =>
        new StaleDraftOrdersSpecification(cutoff).IsSatisfiedBy(this);

    private void RecalculateTotal()
    {
        if (_lines.Count == 0)
        {
            Total = Money.Zero(Currency);
            return;
        }

        var sum = _lines.Sum(l => l.LineTotal.Amount);
        Total = Money.Create(sum, Currency).Value;
    }
}
