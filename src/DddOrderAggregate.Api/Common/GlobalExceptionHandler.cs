using DddOrderAggregate.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DddOrderAggregate.Api.Common;

/// <summary>
/// Global exception handler translating unhandled server exceptions into RFC 9457 ProblemDetails.
/// Specifically intercepts ConcurrencyConflictException and surfaces an HTTP 409 Conflict.
/// </summary>
public sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        LogUnhandledException(_logger, exception);

        var (statusCode, title, code) = exception switch
        {
            ConcurrencyConflictException => (
                StatusCodes.Status409Conflict,
                "Concurrency Conflict",
                "Concurrency.Conflict"),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "Server.Failure")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);

        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "An unhandled exception occurred while processing request.")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);
}
