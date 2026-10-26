namespace DddOrderAggregate.Application.Exceptions;

/// <summary>
/// Thrown when an optimistic concurrency conflict occurs during aggregate persistence.
/// Caught by the global exception handler and translated to an HTTP 409 Conflict ProblemDetails.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("A concurrency conflict occurred. The aggregate was modified by another operation.")
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
