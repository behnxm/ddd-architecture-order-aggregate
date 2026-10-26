namespace DddOrderAggregate.Application.Contracts;

/// <summary>
/// Full aggregate state representation returned to HTTP clients.
/// Decoupled from internal domain types by projecting strictly into primitives.
/// </summary>
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    string Currency,
    decimal Total,
    AddressResponse? ShippingAddress,
    IReadOnlyList<OrderLineResponse> Lines,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    int Version);
