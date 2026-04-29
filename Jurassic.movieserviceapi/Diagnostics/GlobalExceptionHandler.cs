using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Jurassic.movieserviceapi.Diagnostics;

/// <summary>Maps database failures to consistent 503 responses and logs; other errors to 500.</summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
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
        if (!IsDatabaseRelated(exception))
        {
            return false;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        _logger.LogError(
            exception,
            "Database operation failed for {Method} {Path}. TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path.Value,
            traceId);

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
        problem.Extensions["traceId"] = traceId;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        return true;
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
