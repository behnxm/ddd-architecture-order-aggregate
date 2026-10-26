using System.Text.RegularExpressions;
using DddOrderAggregate.Domain.Orders.Errors;
using DddOrderAggregate.Domain.SharedKernel.Results;

namespace DddOrderAggregate.Domain.Orders.ValueObjects;

/// <summary>
/// Immutable value object representing a physical shipping address.
/// Enforces length constraints, non-empty fields, and strict 2-letter ISO country codes.
/// Validates all fields simultaneously and aggregates failures into a ValidationError.
/// </summary>
public sealed partial record Address
{
    public const int MaxStreetLength = 100;
    public const int MaxCityLength = 60;
    public const int MaxPostalCodeLength = 20;

    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string CountryCode { get; }

    private Address(string street, string city, string postalCode, string countryCode)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
        CountryCode = countryCode;
    }

    // Private copy constructor prevents external 'with' expressions from bypassing domain validation
    private Address(Address original)
    {
        Street = original.Street;
        City = original.City;
        PostalCode = original.PostalCode;
        CountryCode = original.CountryCode;
    }

    public static Result<Address> Create(string? street, string? city, string? postalCode, string? countryCode)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(street) || street.Trim().Length > MaxStreetLength)
        {
            errors.Add(OrderErrors.InvalidAddressStreet);
        }

        if (string.IsNullOrWhiteSpace(city) || city.Trim().Length > MaxCityLength)
        {
            errors.Add(OrderErrors.InvalidAddressCity);
        }

        if (string.IsNullOrWhiteSpace(postalCode) || postalCode.Trim().Length > MaxPostalCodeLength)
        {
            errors.Add(OrderErrors.InvalidAddressPostalCode);
        }

        var normalizedCountry = countryCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalizedCountry.Length != 2 || !CountryCodeRegex().IsMatch(normalizedCountry))
        {
            errors.Add(OrderErrors.InvalidAddressCountryCode);
        }

        if (errors.Count > 0)
        {
            return Result.Failure<Address>(new ValidationError(errors));
        }

        return Result.Success(new Address(
            street!.Trim(),
            city!.Trim(),
            postalCode!.Trim(),
            normalizedCountry));
    }

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex CountryCodeRegex();

    public override string ToString() => $"{Street}, {City}, {PostalCode}, {CountryCode}";
}
