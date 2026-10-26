namespace DddOrderAggregate.Domain.SharedKernel;

/// <summary>
/// Represents an event that occurred within the domain boundary.
/// In Domain-Driven Design, domain events capture state-changing occurrences
/// that domain experts care about, establishing auditability and decoupling side effects.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this occurrence, generated using sequential UUIDv7.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// The timestamp when the event occurred in UTC.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
