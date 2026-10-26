using System.Collections.Concurrent;
using System.Reflection;
using DddOrderAggregate.Application.Ports;
using DddOrderAggregate.Domain.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace DddOrderAggregate.Application.Common;

/// <summary>
/// Dispatches domain events to their corresponding generic IDomainEventHandler implementations.
/// Uses a cached delegate dictionary so reflection to construct generic handlers only executes once per event type.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IDomainEvent, CancellationToken, Task>> InvokerCache = new();
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var invoker = InvokerCache.GetOrAdd(domainEvent.GetType(), CreateInvoker);
        await invoker(_serviceProvider, domainEvent, cancellationToken).ConfigureAwait(false);
    }

    private static Func<IServiceProvider, IDomainEvent, CancellationToken, Task> CreateInvoker(Type eventType)
    {
        var method = typeof(DomainEventDispatcher)
            .GetMethod(nameof(DispatchInternalAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(eventType);

        return (sp, evt, ct) => (Task)method.Invoke(null, [sp, evt, ct])!;
    }

    private static async Task DispatchInternalAsync<TEvent>(
        IServiceProvider serviceProvider,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        var handlers = serviceProvider.GetServices<IDomainEventHandler<TEvent>>();
        var typedEvent = (TEvent)domainEvent;

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(typedEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
