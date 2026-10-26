using System.ComponentModel.DataAnnotations;

namespace DddOrderAggregate.Api.Requests;

/// <summary>
/// Payload to initialize a new order in Draft status.
/// </summary>
public sealed record CreateOrderRequest(
    [Required] Guid CustomerId,
    [Required, StringLength(3, MinimumLength = 3)] string Currency);

/// <summary>
/// Payload to add an item to an active draft order.
/// </summary>
public sealed record AddOrderLineRequest(
    [Required] Guid ProductId,
    [Range(1, 100)] int Quantity);

/// <summary>
/// Payload to adjust the quantity of an existing line item.
/// </summary>
public sealed record ChangeOrderLineQuantityRequest(
    [Range(1, 100)] int Quantity);

/// <summary>
/// Payload to set the delivery destination on a draft order.
/// </summary>
public sealed record SetShippingAddressRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Street,
    [Required, StringLength(60, MinimumLength = 1)] string City,
    [Required, StringLength(20, MinimumLength = 1)] string PostalCode,
    [Required, RegularExpression("^[A-Z]{2}$", ErrorMessage = "Country code must be exactly 2 uppercase letters.")] string CountryCode);

/// <summary>
/// Payload to cancel an order with a mandatory business explanation.
/// </summary>
public sealed record CancelOrderRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Reason);
