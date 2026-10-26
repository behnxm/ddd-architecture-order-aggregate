# 0006. Result Pattern for Business Errors, Exceptions for Bugs

## Context
Handling expected business rule violations (such as submitting an empty order, adding a line after submission, or exceeding maximum line counts) via thrown exceptions introduces significant performance overhead, obscures domain control flow, and turns expected domain outcomes into exceptional stack-trace crashes.

## Decision
I established an explicit split between expected domain outcomes and system-level bugs:
1. **Result Pattern for Business Errors**: Expected domain operations return `Result` or `Result<T>`. An operation that violates a domain invariant (e.g. attempting to modify an order in `Submitted` status) returns `Result.Failure(OrderErrors.NotDraft(status))`.
2. **Exceptions for Programmer Bugs and Corrupt Data**: Guard clauses that detect programmer errors (e.g. passing a `null` argument into an internal constructor) or impossible states throw standard exceptions (`ArgumentNullException`, `InvalidOperationException`).
3. **HTTP Mapping**: At the API boundary, `ResultExtensions.ToHttpResult` translates domain `ErrorType` values directly into RFC 9457 ProblemDetails:
   - `Validation` &rarr; 400 Bad Request (with detailed error list)
   - `NotFound` &rarr; 404 Not Found
   - `Conflict` &rarr; 409 Conflict
   - `PreconditionFailed` &rarr; 412 Precondition Failed
   - `Failure` &rarr; 500 Internal Server Error

## Consequences
- **Positive**: Domain method signatures explicitly document that failure is a possible, expected outcome.
- **Positive**: Eliminates expensive exception-throwing overhead on expected business validation paths.
- **Positive**: Clean separation between predictable domain logic and actual infrastructural/system failures.
