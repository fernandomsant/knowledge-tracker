# Invalid or missing workspace headers become server errors

Affected file:

- `src/KnowledgeTracker/KnowledgeTracker.Web/Authentication/Services/CurrentWorkspaceContext.cs:17` — throws for missing or malformed `X-Workspace-Id`; normal API requests have no centralized exception-to-ProblemDetails handler.

Improvement suggestion: add global API exception handling that maps missing workspace selection to 401, malformed headers to 400, and unexpected failures to a safe 500 ProblemDetails response. Do not implement until requested.
