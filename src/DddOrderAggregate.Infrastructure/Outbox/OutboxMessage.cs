namespace DddOrderAggregate.Infrastructure.Outbox;

/// <summary>
/// Persistence entity representing a message enqueued in the Transactional Outbox table.
/// Guarantees at-least-once delivery by participating in the same database transaction as domain state mutations.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public string ContractName { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTimeOffset OccurredOn { get; init; }
    public DateTimeOffset? ProcessedOn { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private OutboxMessage()
    {
    }

    public OutboxMessage(
        Guid id,
        string contractName,
        string payload,
        DateTimeOffset occurredOn)
    {
        Id = id;
        ContractName = contractName;
        Payload = payload;
        OccurredOn = occurredOn;
        Attempts = 0;
    }
}
