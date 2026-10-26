using System.Linq.Expressions;

namespace DddOrderAggregate.Domain.SharedKernel.Specifications;

/// <summary>
/// Encapsulates a domain predicate or query criteria that can be evaluated against
/// in-memory domain objects or translated to database queries via LINQ Expression trees.
/// </summary>
/// <typeparam name="T">The candidate type evaluated by the specification.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the LINQ expression defining the domain rule criteria.
    /// </summary>
    Expression<Func<T, bool>> Criteria { get; }

    /// <summary>
    /// Evaluates whether an in-memory entity satisfies the specification criteria.
    /// </summary>
    /// <param name="entity">The candidate entity.</param>
    /// <returns>True if the criteria is satisfied; otherwise false.</returns>
    bool IsSatisfiedBy(T entity);
}
