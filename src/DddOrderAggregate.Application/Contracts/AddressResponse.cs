namespace DddOrderAggregate.Application.Contracts;

/// <summary>
/// Delivery address response representation using primitive values.
/// </summary>
public sealed record AddressResponse(
    string Street,
    string City,
    string PostalCode,
    string CountryCode);
