using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KnowledgeTracker.Web.Knowledge.Filters;

public sealed class McpExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        context.Result = context.Exception switch
        {
            UnauthorizedAccessException => new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "MCP authorization failed.",
                Detail = "The MCP access token does not grant this operation."
            }) { StatusCode = StatusCodes.Status403Forbidden },
            KeyNotFoundException => new NotFoundObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found."
            }),
            ArgumentException => new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid MCP request."
            }),
            InvalidOperationException => new ConflictObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "MCP operation conflicts with the current application state."
            }),
            _ => null
        };

        context.ExceptionHandled = context.Result is not null;
    }
}
