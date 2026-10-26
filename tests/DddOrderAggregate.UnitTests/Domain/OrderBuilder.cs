using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.Identifiers;
using DddOrderAggregate.Domain.Orders.ValueObjects;

namespace DddOrderAggregate.UnitTests.Domain;

/// <summary>
/// Test Data Builder for creating Order aggregate instances with sensible defaults.
/// </summary>
public sealed class OrderBuilder
{
    private CustomerId _customerId = CustomerId.New();
    private Currency _currency = Currency.USD;
    private DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly List<(ProductId ProductId, string ProductName, Money UnitPrice, Quantity Quantity)> _lines = [];
    private Address? _shippingAddress;

    public static OrderBuilder Create() => new();

    public OrderBuilder WithCustomer(CustomerId customerId)
    {
        _customerId = customerId;
        return this;
    }

    public OrderBuilder WithCurrency(Currency currency)
    {
        _currency = currency;
        return this;
    }

    public OrderBuilder WithTime(DateTimeOffset now)
    {
        _now = now;
        return this;
    }

    public OrderBuilder WithLine(ProductId productId, string productName, Money unitPrice, Quantity quantity)
    {
        _lines.Add((productId, productName, unitPrice, quantity));
        return this;
    }

    public OrderBuilder WithStandardLine()
    {
        _lines.Add((
            ProductId.New(),
            "Mechanical Keyboard",
            Money.Create(149.99m, _currency).Value,
            Quantity.Create(1).Value));
        return this;
    }

    public OrderBuilder WithShippingAddress(Address address)
    {
        _shippingAddress = address;
        return this;
    }

    public OrderBuilder WithStandardShippingAddress()
    {
        _shippingAddress = Address.Create("123 Market St", "San Francisco", "94105", "US").Value;
        return this;
    }

    public Order Build()
    {
        var order = Order.CreateDraft(_customerId, _currency, _now).Value;

        foreach (var (productId, productName, unitPrice, quantity) in _lines)
        {
            order.AddLine(productId, productName, unitPrice, quantity, _now);
        }

        if (_shippingAddress is not null)
        {
            order.SetShippingAddress(_shippingAddress, _now);
        }

        return order;
    }
}
