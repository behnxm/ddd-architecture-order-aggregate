namespace DddOrderAggregate.Domain.SharedKernel;

/// <summary>
/// Marker interface defining an Aggregate Root.
/// In Domain-Driven Design, aggregate roots define consistency boundaries.
/// External objects are only permitted to hold references to the aggregate root,
/// never directly to internal entities or child objects.
/// </summary>
public interface IAggregateRoot
{
}
