namespace DddOrderAggregate.Domain.SharedKernel;

/// <summary>
/// Encapsulates atomic persistence of aggregate mutations and state changes within the boundary.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all pending changes across aggregate boundaries and outbox records atomically.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the underlying storage.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
