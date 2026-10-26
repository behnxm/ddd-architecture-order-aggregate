namespace DddOrderAggregate.Domain.SharedKernel;

/// <summary>
/// Base class for domain entities defined by their identity rather than structural attributes.
/// Two entities are equal if and only if they share the exact runtime type and non-default identifier.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier type.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Gets the unique identity of this entity.
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Parameterless constructor required by EF Core.
    /// </summary>
    protected Entity()
    {
    }

    /// <summary>
    /// Initializes an entity with its domain identity.
    /// </summary>
    /// <param name="id">The strongly-typed identity.</param>
    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
