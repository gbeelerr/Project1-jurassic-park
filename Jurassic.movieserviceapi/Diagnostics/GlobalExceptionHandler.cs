using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Jurassic.movieserviceapi.Diagnostics;

/// <summary>Maps database failures to consistent responses: unique violations to 409, connectivity/query failures to 503.</summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    /// <summary>PostgreSQL <c>unique_violation</c>; must return 409 for duplicate registration, not 503.</summary>
    private const string PostgresUniqueViolationSqlState = "23505";

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var pg = FindPostgresException(exception);
        if (pg is { SqlState: PostgresUniqueViolationSqlState })
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            _logger.LogInformation(
                "Unique constraint violation for {Method} {Path}. TraceId: {TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path.Value,
                traceId);

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            httpContext.Response.ContentType = "application/problem+json";

            var conflict = new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Detail = _environment.IsDevelopment()
                    ? pg.MessageText
                    : "The request conflicts with the current state of the resource (e.g. duplicate email).",
                Instance = httpContext.Request.Path.Value
            };
            conflict.Extensions["traceId"] = traceId;

            await httpContext.Response.WriteAsJsonAsync(conflict, cancellationToken: cancellationToken);
            return true;
        }

        if (!IsDatabaseRelated(exception))
        {
            return false;
        }

        var traceId503 = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        _logger.LogError(
            exception,
            "Database operation failed for {Method} {Path}. TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            traceId503);

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Title = "Service unavailable",
            Status = StatusCodes.Status503ServiceUnavailable,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            Detail = _environment.IsDevelopment()
                ? exception.GetBaseException().Message
                : "The database could not be reached. Please try again later.",
            Instance = httpContext.Request.Path.Value
        };
        problem.Extensions["traceId"] = traceId503;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        return true;
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (var ex = exception; ex != null; ex = ex.InnerException)
        {
            if (ex is PostgresException p)
            {
                return p;
            }
        }

        return null;
    }

    private static bool IsDatabaseRelated(Exception exception)
    {
        for (var ex = exception; ex != null; ex = ex.InnerException)
        {
            if (ex is NpgsqlException)
            {
                return true;
            }
        }

        return false;
    }
}
