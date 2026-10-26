namespace DddOrderAggregate.Domain.SharedKernel.Results;

/// <summary>
/// Categorization of domain error types mapping directly to HTTP semantics at the API edge.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// An unexpected or internal operational failure.
    /// </summary>
    Failure = 0,

    /// <summary>
    /// A domain invariant or value constraint was violated by user input.
    /// </summary>
    Validation = 1,

    /// <summary>
    /// The requested aggregate or resource was not found.
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// The requested operation conflicts with the current lifecycle state of the aggregate.
    /// </summary>
    Conflict = 3,

    /// <summary>
    /// A precondition such as optimistic concurrency version failed.
    /// </summary>
    PreconditionFailed = 4
}
