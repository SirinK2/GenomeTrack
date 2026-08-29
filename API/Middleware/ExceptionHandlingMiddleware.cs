using System;
using System.Text.Json;
using System.Threading.Tasks;
using GenomeTrack.Application.Response;

namespace GenomeTrack.API.Middleware;

/// <summary>
/// Turns anything that escapes a controller into the same envelope every endpoint returns, so a
/// client never has to parse two shapes. The exception detail is logged, not sent — a stack
/// trace in a response body tells an attacker about the internals for free.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            // The append-only guard in AppDbContext throws this. It is a rule violation by the
            // caller's request, not a server fault, so it answers 409 rather than 500.
            _logger.LogWarning(ex, "Rejected an operation that violated an invariant.");
            await WriteAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Path}.", context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(
                Result.Failure(message),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            )
        );
    }
}
