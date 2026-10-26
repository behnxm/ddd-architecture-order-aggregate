using DddOrderAggregate.Domain.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace DddOrderAggregate.Api.Common;

/// <summary>
/// Maps domain Error abstractions to RFC 9457 ProblemDetails HTTP responses.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Error error, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(httpContext);

        var (statusCode, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation Failed"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.PreconditionFailed => (StatusCodes.Status412PreconditionFailed, "Precondition Failed"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var extensions = new Dictionary<string, object?>
        {
            ["code"] = error.Code,
            ["traceId"] = httpContext.TraceIdentifier
        };

        if (error is ValidationError validationError)
        {
            extensions["errors"] = validationError.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
        }

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: error.Description,
            extensions: extensions);
    }
}
