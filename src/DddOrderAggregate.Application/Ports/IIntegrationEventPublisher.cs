using DddOrderAggregate.Application.IntegrationEvents;

namespace DddOrderAggregate.Application.Ports;

/// <summary>
/// Port for publishing integration events out to external messaging infrastructure or message brokers.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes an integration event to external consumers.
    /// </summary>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
