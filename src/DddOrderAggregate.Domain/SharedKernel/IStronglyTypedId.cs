namespace DddOrderAggregate.Domain.SharedKernel;

/// <summary>
/// Contract for strongly-typed identifiers implemented by readonly record structs.
/// Prevents primitive obsession and argument transposition bugs (e.g. passing CustomerId where OrderId was expected).
/// </summary>
/// <typeparam name="TSelf">The concrete struct identifier type implementing the contract.</typeparam>
public interface IStronglyTypedId<TSelf> where TSelf : struct, IStronglyTypedId<TSelf>
{
    /// <summary>
    /// The underlying raw GUID value.
    /// </summary>
    Guid Value { get; }

    /// <summary>
    /// Creates a strongly-typed identifier instance from an existing valid GUID.
    /// </summary>
    /// <param name="value">A non-empty GUID.</param>
    /// <returns>An instance of <typeparamref name="TSelf"/>.</returns>
    static abstract TSelf From(Guid value);
}
