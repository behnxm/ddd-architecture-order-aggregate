using DddOrderAggregate.Application.IntegrationEvents;

namespace DddOrderAggregate.Application.Ports;

/// <summary>
/// Port for enqueuing integration events into the transactional outbox table.
/// Enqueued messages share the database transaction with aggregate state changes.
/// </summary>
public interface IOutbox
{
    /// <summary>
    /// Enqueues an integration event to be committed in the active unit of work transaction.
    /// </summary>
    void Enqueue(IIntegrationEvent integrationEvent);
}
