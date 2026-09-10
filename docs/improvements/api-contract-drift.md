# The two API surfaces can drift

Affected files:

- `src/KnowledgeTracker/KnowledgeTracker.Web/Knowledge/Controllers` — normal `/api/*` HTTP contracts.
- `src/KnowledgeTracker/KnowledgeTracker.Web/Knowledge/Controllers/McpKnowledgeController.cs` — dedicated `/mcp-api/*` contracts.
- `src/KnowledgeTracker/KnowledgeTracker.Tests/Mcp/KnowledgeToolsContractTests.cs` — contract assertions.

The normal and MCP controllers share application services but duplicate request/response contracts. The failing MCP parameter test is evidence that one surface can change without the other.

Improvement suggestion: share contract records or generate MCP schemas from the application contract map, then keep a focused compatibility test for intentional differences. Do not implement until requested.
