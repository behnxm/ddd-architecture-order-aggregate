using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DddOrderAggregate.Infrastructure.Interceptors;

/// <summary>
/// SaveChangesInterceptor that dispatches domain events within the same database transaction.
/// 
/// Trade-offs:
/// - Handlers participate in the active unit of work transaction, meaning outbox messages and aggregate state commit atomically.
/// - If a domain event handler fails, the entire transaction is rolled back, preventing partial inconsistencies.
/// - Handlers MUST NEVER invoke SaveChangesAsync directly to prevent re-entrant interceptor loops.
/// - Guarded with a maximum recursive depth to abort circular cascades safely.
/// </summary>
public sealed class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    public const int MaxRecursionDepth = 10;
    private readonly IDomainEventDispatcher _dispatcher;

    public DispatchDomainEventsInterceptor(IDomainEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var depth = 0;
        while (depth < MaxRecursionDepth)
        {
            var aggregatesWithEvents = context.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Select(e => e.Entity)
                .Where(e => e.DomainEvents.Count > 0)
                .ToList();

            if (aggregatesWithEvents.Count == 0)
            {
                break;
            }

            depth++;
            if (depth >= MaxRecursionDepth)
            {
                throw new InvalidOperationException(
                    $"Domain event dispatch exceeded maximum recursive depth of {MaxRecursionDepth}. Aborting to prevent runaway recursion.");
            }

            var eventsToDispatch = new List<IDomainEvent>();
            foreach (var aggregate in aggregatesWithEvents)
            {
                eventsToDispatch.AddRange(aggregate.DomainEvents);
                aggregate.ClearDomainEvents();
            }

            foreach (var domainEvent in eventsToDispatch)
            {
                await _dispatcher.DispatchAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            }

            context.ChangeTracker.DetectChanges();
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
