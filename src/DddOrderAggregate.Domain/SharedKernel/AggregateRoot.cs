namespace DddOrderAggregate.Domain.SharedKernel;

/// <summary>
/// Defines an entity or aggregate that records and exposes uncommitted domain events.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the list of domain events recorded by this aggregate.
    /// </summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all recorded domain events.
    /// </summary>
    void ClearDomainEvents();
}

/// <summary>
/// Base class for Aggregate Roots in tactical Domain-Driven Design.
/// Aggregate roots act as transaction and consistency gateways for all enclosed entities.
/// They are responsible for recording domain events and managing optimistic concurrency versions.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier type.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the list of domain events raised by state mutations within this aggregate boundary.
    /// Cleared once published or persisted by the unit of work.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Optimistic concurrency token and revision number for this aggregate instance.
    /// Initialized to 1 upon creation and monotonically incremented with every mutation.
    /// </summary>
    public int Version { get; protected set; } = 1;

    /// <summary>
    /// The timestamp when the aggregate was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; protected set; }

    /// <summary>
    /// The timestamp of the most recent state change.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; protected set; }

    /// <summary>
    /// Parameterless constructor for EF Core materialization.
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Initializes an aggregate root with its identity and initial timestamps.
    /// </summary>
    /// <param name="id">The strongly-typed identity.</param>
    /// <param name="createdAt">The creation timestamp.</param>
    protected AggregateRoot(TId id, DateTimeOffset createdAt) : base(id)
    {
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Version = 1;
    }

    /// <summary>
    /// Records a new domain event to be dispatched when the unit of work commits.
    /// </summary>
    /// <param name="domainEvent">The domain event instance.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all recorded domain events. Invoked by the persistence interceptor after collection.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Increments the aggregate version and updates the timestamp when a successful domain mutation occurs.
    /// </summary>
    /// <param name="now">The current timestamp.</param>
    protected void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }
}
