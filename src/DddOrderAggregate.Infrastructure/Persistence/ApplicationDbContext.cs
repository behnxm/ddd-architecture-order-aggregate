using System.Reflection;
using DddOrderAggregate.Application.Exceptions;
using DddOrderAggregate.Application.IntegrationEvents;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.Orders;
using DddOrderAggregate.Domain.Orders.ValueObjects;
using DddOrderAggregate.Domain.SharedKernel;
using DddOrderAggregate.Infrastructure.Outbox;
using DddOrderAggregate.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DddOrderAggregate.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext implementing IUnitOfWork and IOutbox.
/// Configured without a DbSet for OrderLine to enforce aggregate boundary encapsulation.
/// Catches DbUpdateConcurrencyException and translates it into ConcurrencyConflictException.
/// </summary>
public sealed class ApplicationDbContext : DbContext, IUnitOfWork, IOutbox
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public void Enqueue(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var payload = OutboxRegistry.Serialize(integrationEvent);
        var message = new OutboxMessage(
            integrationEvent.MessageId,
            integrationEvent.ContractName,
            payload,
            integrationEvent.OccurredOn);

        OutboxMessages.Add(message);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "A database concurrency conflict occurred. The aggregate was modified concurrently by another transaction.",
                ex);
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // Dynamically discover and register StronglyTypedIdConverter for all IStronglyTypedId structs
        var stronglyTypedIdInterface = typeof(IStronglyTypedId<>);
        var domainAssembly = typeof(Order).Assembly;

        var idTypes = domainAssembly.GetTypes()
            .Where(t => t is { IsValueType: true } &&
                        t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == stronglyTypedIdInterface));

        foreach (var idType in idTypes)
        {
            var converterType = typeof(StronglyTypedIdConverter<>).MakeGenericType(idType);
            configurationBuilder.Properties(idType).HaveConversion(converterType);
        }

        // Value converters for domain value objects
        configurationBuilder.Properties<Currency>().HaveConversion<CurrencyConverter>();
        configurationBuilder.Properties<Quantity>().HaveConversion<QuantityConverter>();

        // DateTimeOffset conversion for deterministic SQLite ordering
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
