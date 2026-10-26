using DddOrderAggregate.Domain.SharedKernel;

namespace DddOrderAggregate.Application.Ports;

/// <summary>
/// Dispatches domain events collected from aggregates to registered IDomainEventHandler instances.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a domain event to all registered handlers in the service container.
    /// </summary>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
