using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DddOrderAggregate.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for the Order aggregate root.
/// Maps Total as a complex property, ShippingAddress as an optional complex property,
/// and Lines as an owned collection with field access mode and AutoInclude.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.Currency)
            .HasConversion<CurrencyConverter>()
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(o => o.CancellationReason)
            .HasMaxLength(Order.MaxCancellationReasonLength);

        // Optimistic concurrency token
        builder.Property(o => o.Version)
            .IsConcurrencyToken();

        // Total mapped as a complex property
        builder.ComplexProperty(o => o.Total, tb =>
        {
            tb.Property(m => m.Amount)
                .HasConversion<MinorUnitMoneyConverter>()
                .HasColumnName("total_amount_minor_units")
                .IsRequired();

            tb.Property(m => m.Currency)
                .HasConversion<CurrencyConverter>()
                .HasMaxLength(3)
                .HasColumnName("total_currency")
                .IsRequired();
        });

        // ShippingAddress mapped as an optional complex property
        builder.ComplexProperty(o => o.ShippingAddress, ab =>
        {
            ab.Property(a => a.Street)
                .HasMaxLength(Address.MaxStreetLength)
                .HasColumnName("shipping_street");

            ab.Property(a => a.City)
                .HasMaxLength(Address.MaxCityLength)
                .HasColumnName("shipping_city");

            ab.Property(a => a.PostalCode)
                .HasMaxLength(Address.MaxPostalCodeLength)
                .HasColumnName("shipping_postal_code");

            ab.Property(a => a.CountryCode)
                .HasMaxLength(2)
                .HasColumnName("shipping_country_code");
        });

        // Lines mapped as an owned collection
        builder.OwnsMany(o => o.Lines, lb =>
        {
            lb.ToTable("order_lines");
            lb.WithOwner().HasForeignKey("OrderId");
            lb.HasKey(l => l.Id);
            lb.Property(l => l.Id).ValueGeneratedNever();

            lb.Property(l => l.ProductName)
                .HasMaxLength(OrderLine.MaxProductNameLength)
                .IsRequired();

            lb.Property(l => l.Quantity)
                .HasConversion<QuantityConverter>()
                .IsRequired();

            // UnitPrice mapped via nested OwnsOne on the owned line entity
            lb.OwnsOne(l => l.UnitPrice, ub =>
            {
                ub.Property(m => m.Amount)
                    .HasConversion<MinorUnitMoneyConverter>()
                    .HasColumnName("unit_price_minor_units")
                    .IsRequired();

                ub.Property(m => m.Currency)
                    .HasConversion<CurrencyConverter>()
                    .HasMaxLength(3)
                    .HasColumnName("unit_price_currency")
                    .IsRequired();
            });
        });

        // Private backing field access mode and AutoInclude so the aggregate always loads whole
        builder.Navigation(o => o.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();
    }
}
