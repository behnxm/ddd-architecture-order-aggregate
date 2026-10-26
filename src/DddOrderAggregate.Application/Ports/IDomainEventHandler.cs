using DddOrderAggregate.Domain.SharedKernel;

namespace DddOrderAggregate.Application.Ports;

/// <summary>
/// Handles domain events raised by aggregates within the local application context.
/// </summary>
/// <typeparam name="TEvent">The concrete domain event type.</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Executes side-effects such as outbox enqueuing or local logging in response to a domain event.
    /// </summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
