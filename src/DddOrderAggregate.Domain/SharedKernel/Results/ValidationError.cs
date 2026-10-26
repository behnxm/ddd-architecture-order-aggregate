namespace DddOrderAggregate.Domain.SharedKernel.Results;

/// <summary>
/// Specialized error representing multiple aggregated validation failures.
/// </summary>
public sealed record ValidationError(IReadOnlyList<Error> Errors)
    : Error("General.Validation", "One or more validation errors occurred.", ErrorType.Validation);
