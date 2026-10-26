namespace DddOrderAggregate.Domain.SharedKernel.Results;

#pragma warning disable CA1716 // The type name 'Error' is explicitly required by the Result pattern specification in domain modeling.
/// <summary>
/// Encapsulates a domain or application error with a stable code, description, and classification.
/// </summary>
public record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error PreconditionFailed(string code, string description) => new(code, description, ErrorType.PreconditionFailed);
}
#pragma warning restore CA1716
