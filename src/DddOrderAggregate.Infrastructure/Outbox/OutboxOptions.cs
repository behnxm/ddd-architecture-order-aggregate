namespace DddOrderAggregate.Infrastructure.Outbox;

/// <summary>
/// Configuration options for the Transactional Outbox processor.
/// </summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int IntervalMilliseconds { get; set; } = 5000;
    public int BatchSize { get; set; } = 20;
    public int MaxAttempts { get; set; } = 5;
}
