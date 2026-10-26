using System.Linq.Expressions;

namespace DddOrderAggregate.Domain.SharedKernel.Specifications;

/// <summary>
/// Abstract base class for specifications providing expression compilation, caching,
/// and boolean composition operators (And, Or, Not).
/// </summary>
/// <typeparam name="T">The candidate type.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    private readonly Lazy<Func<T, bool>> _compiledCriteria;

    protected Specification()
    {
        _compiledCriteria = new Lazy<Func<T, bool>>(() => Criteria.Compile());
    }

    /// <inheritdoc />
    public abstract Expression<Func<T, bool>> Criteria { get; }

    /// <inheritdoc />
    public bool IsSatisfiedBy(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _compiledCriteria.Value(entity);
    }

    /// <summary>
    /// Combines this specification with another using logical AND.
    /// </summary>
    public Specification<T> And(Specification<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new BinarySpecification<T>(this, other, Expression.AndAlso);
    }

    /// <summary>
    /// Combines this specification with another using logical OR.
    /// </summary>
    public Specification<T> Or(Specification<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new BinarySpecification<T>(this, other, Expression.OrElse);
    }

    /// <summary>
    /// Negates this specification using logical NOT.
    /// </summary>
    public Specification<T> Not() => new NotSpecification<T>(this);

    private sealed class BinarySpecification<TType> : Specification<TType>
    {
        private readonly Expression<Func<TType, bool>> _expression;

        public BinarySpecification(
            Specification<TType> left,
            Specification<TType> right,
            Func<Expression, Expression, BinaryExpression> mergeOperator)
        {
            var parameter = Expression.Parameter(typeof(TType), "entity");

            var leftVisitor = new ParameterReplacerVisitor(left.Criteria.Parameters[0], parameter);
            var leftBody = leftVisitor.Visit(left.Criteria.Body)!;

            var rightVisitor = new ParameterReplacerVisitor(right.Criteria.Parameters[0], parameter);
            var rightBody = rightVisitor.Visit(right.Criteria.Body)!;

            var combinedBody = mergeOperator(leftBody, rightBody);
            _expression = Expression.Lambda<Func<TType, bool>>(combinedBody, parameter);
        }

        public override Expression<Func<TType, bool>> Criteria => _expression;
    }

    private sealed class NotSpecification<TType> : Specification<TType>
    {
        private readonly Expression<Func<TType, bool>> _expression;

        public NotSpecification(Specification<TType> inner)
        {
            var parameter = inner.Criteria.Parameters[0];
            var negatedBody = Expression.Not(inner.Criteria.Body);
            _expression = Expression.Lambda<Func<TType, bool>>(negatedBody, parameter);
        }

        public override Expression<Func<TType, bool>> Criteria => _expression;
    }

    private sealed class ParameterReplacerVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly ParameterExpression _target;

        public ParameterReplacerVisitor(ParameterExpression source, ParameterExpression target)
        {
            _source = source;
            _target = target;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _source ? _target : base.VisitParameter(node);
        }
    }
}
