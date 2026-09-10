using Microsoft.AspNetCore.Diagnostics;

namespace KnowledgeTracker.Web.Middleware;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (context.Response.HasStarted)
            return false;

        var (status, title, detail) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Authentication required.", "A valid authentication token and workspace selection are required."),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request.", "The request could not be processed."),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found.", "The requested resource was not found."),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Operation conflict.", "The operation conflicts with the current application state."),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error.", "The server could not complete the request."),
        };

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled API exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning(exception, "API request rejected for {Method} {Path}", context.Request.Method, context.Request.Path);

        await Results.Problem(statusCode: status, title: title, detail: detail, instance: context.Request.Path).ExecuteAsync(context);
        return true;
    }
}
