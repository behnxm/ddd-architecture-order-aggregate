using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel.Results;
using Shouldly;
using Xunit;

namespace DddOrderAggregate.UnitTests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void Money_RejectsNegativeAmount()
    {
        var result = Money.Create(-10.00m, Currency.USD);
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Money.InvalidAmount");
    }

    [Fact]
    public void Money_RejectsMoreThanTwoDecimalPlaces()
    {
        // 10.555 has 3 decimal places -> reject rather than silently round!
        var result = Money.Create(10.555m, Currency.USD);
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Money.InvalidAmount");
    }

    [Fact]
    public void Money_AcceptsValidAmount_AndCalculatesEqualityByValue()
    {
        var m1 = Money.Create(10.50m, Currency.USD).Value;
        var m2 = Money.Create(10.50m, Currency.USD).Value;
        var m3 = Money.Create(10.50m, Currency.EUR).Value;

        m1.ShouldBe(m2);
        (m1 == m2).ShouldBeTrue();
        (m1 == m3).ShouldBeFalse();
    }

    [Fact]
    public void Money_Add_FailsWhenCurrenciesDiffer()
    {
        var usd = Money.Create(10.00m, Currency.USD).Value;
        var eur = Money.Create(5.00m, Currency.EUR).Value;

        var addResult = usd.Add(eur);
        addResult.IsFailure.ShouldBeTrue();
        addResult.Error.Code.ShouldBe("Order.CurrencyMismatch");
    }

    [Fact]
    public void Money_Multiply_CalculatesCorrectProduct()
    {
        var unitPrice = Money.Create(19.99m, Currency.USD).Value;
        var quantity = Quantity.Create(3).Value;

        var total = unitPrice.Multiply(quantity);
        total.Amount.ShouldBe(59.97m);
        total.Currency.ShouldBe(Currency.USD);
    }

    [Fact]
    public void Quantity_RejectsValuesOutside1To100()
    {
        Quantity.Create(0).IsFailure.ShouldBeTrue();
        Quantity.Create(-1).IsFailure.ShouldBeTrue();
        Quantity.Create(101).IsFailure.ShouldBeTrue();

        Quantity.Create(1).IsSuccess.ShouldBeTrue();
        Quantity.Create(100).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Quantity_Add_FailsWhenSumExceeds100()
    {
        var q1 = Quantity.Create(60).Value;
        var q2 = Quantity.Create(45).Value;

        var addResult = q1.Add(q2);
        addResult.IsFailure.ShouldBeTrue();
        addResult.Error.Code.ShouldBe("Quantity.ExceedsLimit");
    }

    [Fact]
    public void Address_AggregatesAllValidationErrors()
    {
        // Street too long, city empty, postal code empty, country code invalid
        var result = Address.Create(
            new string('s', 101),
            "",
            "",
            "USA");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
        var validationError = (ValidationError)result.Error;

        validationError.Errors.Count.ShouldBe(4);
        validationError.Errors.ShouldContain(e => e.Code == "Address.InvalidStreet");
        validationError.Errors.ShouldContain(e => e.Code == "Address.InvalidCity");
        validationError.Errors.ShouldContain(e => e.Code == "Address.InvalidPostalCode");
        validationError.Errors.ShouldContain(e => e.Code == "Address.InvalidCountryCode");
    }

    [Fact]
    public void Address_CreatesValidInstance_WhenConstraintsSatisfied()
    {
        var result = Address.Create("123 Tech Lane", "Austin", "78701", "us");
        result.IsSuccess.ShouldBeTrue();
        result.Value.CountryCode.ShouldBe("US");
        result.Value.Street.ShouldBe("123 Tech Lane");
    }

    [Fact]
    public void Currency_RejectsUnsupportedCode()
    {
        Currency.FromCode("JPY").IsFailure.ShouldBeTrue();
        Currency.FromCode("").IsFailure.ShouldBeTrue();

        Currency.FromCode("usd").IsSuccess.ShouldBeTrue();
        Currency.FromCode("USD").Value.ShouldBe(Currency.USD);
    }

    [Fact]
    public void StronglyTypedIds_RejectEmptyGuid_AndGenerateV7()
    {
        Should.Throw<ArgumentException>(() => OrderId.From(Guid.Empty));
        Should.Throw<ArgumentException>(() => CustomerId.From(Guid.Empty));
        Should.Throw<ArgumentException>(() => ProductId.From(Guid.Empty));
        Should.Throw<ArgumentException>(() => OrderLineId.From(Guid.Empty));

        var id1 = OrderId.New();
        var id2 = OrderId.New();
        id1.Value.ShouldNotBe(Guid.Empty);
        id1.ShouldNotBe(id2);
        id1.Value.Version.ShouldBe(7);
    }
}
