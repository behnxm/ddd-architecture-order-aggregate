using DddOrderAggregate.Domain.Orders.Errors;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Domain.Orders;

/// <summary>
/// Internal entity within the Order aggregate boundary.
/// Captures an immutable snapshot of product details (name and unit price) at the time of purchase.
/// Mutations are scoped as internal so state can only be modified through the Order aggregate root.
/// </summary>
public sealed class OrderLine : Entity<OrderLineId>
{
    public const int MaxProductNameLength = 200;

    public ProductId ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public Money UnitPrice { get; private set; } = default!;
    public Quantity Quantity { get; private set; }

    /// <summary>
    /// Computes the total monetary price for this line (UnitPrice * Quantity).
    /// </summary>
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    /// <summary>
    /// Parameterless constructor for EF Core materialization.
    /// </summary>
    private OrderLine()
    {
    }

    internal OrderLine(
        OrderLineId id,
        ProductId productId,
        string productName,
        Money unitPrice,
        Quantity quantity) : base(id)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    internal static Result<OrderLine> Create(
        ProductId productId,
        string productName,
        Money unitPrice,
        Quantity quantity)
    {
        if (string.IsNullOrWhiteSpace(productName) || productName.Trim().Length > MaxProductNameLength)
        {
            return Result.Failure<OrderLine>(OrderErrors.InvalidProductName);
        }

        var line = new OrderLine(
            OrderLineId.New(),
            productId,
            productName.Trim(),
            unitPrice,
            quantity);

        return Result.Success(line);
    }

    internal Result ChangeQuantity(Quantity newQuantity)
    {
        Quantity = newQuantity;
        return Result.Success();
    }

    internal Result MergeQuantity(Quantity additionalQuantity)
    {
        var result = Quantity.Add(additionalQuantity);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        Quantity = result.Value;
        return Result.Success();
    }
}
