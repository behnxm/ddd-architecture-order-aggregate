using System.Linq.Expressions;
using System.Reflection;
using DddOrderAggregate.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DddOrderAggregate.Infrastructure.Persistence.Converters;

/// <summary>
/// Generic EF Core ValueConverter for strongly-typed identifiers.
/// Eliminates boilerplate by dynamically converting any IStronglyTypedId struct to/from Guid.
/// Dynamically compiles expression trees to avoid CS8927 with static abstract interface calls.
/// </summary>
public sealed class StronglyTypedIdConverter<TId> : ValueConverter<TId, Guid>
    where TId : struct, IStronglyTypedId<TId>
{
    public StronglyTypedIdConverter()
        : base(ToExpression(), FromExpression())
    {
    }

    private static Expression<Func<TId, Guid>> ToExpression()
    {
        var param = Expression.Parameter(typeof(TId), "id");
        var property = Expression.Property(param, nameof(IStronglyTypedId<TId>.Value));
        return Expression.Lambda<Func<TId, Guid>>(property, param);
    }

    private static Expression<Func<Guid, TId>> FromExpression()
    {
        var method = typeof(TId).GetMethod(nameof(IStronglyTypedId<TId>.From), BindingFlags.Public | BindingFlags.Static, [typeof(Guid)])!;
        var param = Expression.Parameter(typeof(Guid), "value");
        var call = Expression.Call(method, param);
        return Expression.Lambda<Func<Guid, TId>>(call, param);
    }
}
