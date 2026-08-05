namespace FileStorage.Api.Middleware;

using System.Text.Json;

/// <summary>
/// Converts domain-layer exceptions into clean HTTP responses:
///   InvalidOperationException -> 400 Bad Request
///   UnauthorizedAccessException -> 403 Forbidden
///   everything else -> 500 (logged)
/// </summary>
public sealed class ExceptionHandlingMiddleware
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
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var body = JsonSerializer.Serialize(new
        {
            type = "about:blank",
            title = statusCode == 400 ? "Bad Request" : statusCode == 403 ? "Forbidden" : "Internal Server Error",
            status = statusCode,
            detail,
        });

        await context.Response.WriteAsync(body);
    }
}