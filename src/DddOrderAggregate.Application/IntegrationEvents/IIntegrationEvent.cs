namespace DddOrderAggregate.Application.IntegrationEvents;

/// <summary>
/// Public asynchronous message contract published across bounded context boundaries via the Transactional Outbox.
/// Unlike internal domain events, integration events are stable, schema-versioned, and decoupled from internal domain structures.
/// </summary>
public interface IIntegrationEvent
{
    Guid MessageId { get; }
    DateTimeOffset OccurredOn { get; }
    string ContractName { get; }
}
