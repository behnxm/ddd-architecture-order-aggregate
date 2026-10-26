using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Domain.Orders.Errors;

/// <summary>
/// Domain error factories defining stable error codes and appropriate HTTP-aligned error classifications.
/// </summary>
public static class OrderErrors
{
    public static Error NotFound(OrderId orderId) =>
        Error.NotFound("Order.NotFound", $"Order '{orderId.Value}' was not found.");

    public static Error NotDraft(OrderStatus status) =>
        Error.Conflict("Order.NotDraft", $"Order modifications are only permitted while in Draft status. Current status is '{status}'.");

    public static Error Empty =>
        Error.Conflict("Order.Empty", "An empty order with zero line items cannot be submitted.");

    public static Error LineNotFound(OrderLineId lineId) =>
        Error.NotFound("Order.LineNotFound", $"Order line '{lineId.Value}' does not exist on this order.");

    public static Error TooManyLines(int maxLines) =>
        Error.Conflict("Order.TooManyLines", $"Orders cannot contain more than {maxLines} distinct lines.");

    public static Error CurrencyMismatch(string expectedCurrency, string actualCurrency) =>
        Error.Validation("Order.CurrencyMismatch", $"Currency mismatch: expected '{expectedCurrency}', but received '{actualCurrency}'.");

    public static Error AddressRequired =>
        Error.Conflict("Order.AddressRequired", "A validated shipping address is required before an order can be submitted.");

    public static Error InvalidTransition(OrderStatus from, OrderStatus to) =>
        Error.Conflict("Order.InvalidTransition", $"Cannot transition order from '{from}' to '{to}'.");

    public static Error VersionMismatch(int expectedVersion, int actualVersion) =>
        Error.PreconditionFailed("Order.VersionMismatch", $"Concurrency conflict: expected version {expectedVersion}, but current version is {actualVersion}.");

    public static Error InvalidCancellationReason =>
        Error.Validation("Order.InvalidCancellationReason", "A cancellation reason between 1 and 200 characters is required.");

    public static Error InvalidQuantity(int value) =>
        Error.Validation("Quantity.Invalid", $"Quantity must be between 1 and 100. Provided value was {value}.");

    public static Error QuantityExceedsLimit(int sum) =>
        Error.Validation("Quantity.ExceedsLimit", $"Combined line quantity {sum} exceeds the maximum limit of 100.");

    public static Error InvalidAmount(decimal amount) =>
        Error.Validation("Money.InvalidAmount", $"Amount must be non-negative with at most 2 decimal places. Provided value was {amount}.");

    public static Error InvalidCurrency(string? code) =>
        Error.Validation("Currency.Invalid", $"Currency code '{code}' is not supported. Supported currencies are USD, EUR, GBP.");

    public static Error InvalidAddressStreet =>
        Error.Validation("Address.InvalidStreet", "Street is required and must be between 1 and 100 characters.");

    public static Error InvalidAddressCity =>
        Error.Validation("Address.InvalidCity", "City is required and must be between 1 and 60 characters.");

    public static Error InvalidAddressPostalCode =>
        Error.Validation("Address.InvalidPostalCode", "Postal code is required and must be between 1 and 20 characters.");

    public static Error InvalidAddressCountryCode =>
        Error.Validation("Address.InvalidCountryCode", "Country code must consist of exactly 2 uppercase letters.");

    public static Error InvalidProductName =>
        Error.Validation("OrderLine.InvalidProductName", "Product name must be between 1 and 200 characters.");
}
