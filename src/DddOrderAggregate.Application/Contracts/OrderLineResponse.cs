namespace DddOrderAggregate.Application.Contracts;

/// <summary>
/// Line item response representation using primitive values.
/// </summary>
public sealed record OrderLineResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
