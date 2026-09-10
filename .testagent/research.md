# Phase 4 MCP token security and delivery test research

## Bounded target

- Application token issuing and management use cases.
- Infrastructure token generation, hashing, validation, persistence, expiry, and revocation.
- Web token-management API and normal access-token authentication compatibility.
- MCP tool scope checks and application-level authorization.
- Data repositories and migration ownership filters for authenticated user data spaces.

## Existing conventions

- SDK-style .NET 10 test project with xUnit and VSTest.
- Test doubles are small nested fakes that record calls and persisted state.
- API tests invoke controllers directly and inspect typed action results.
- MCP adapter tests invoke `KnowledgeTools` directly through application-contract interfaces.
- SQL migrations are immutable, sequential files without `GO` batches.

## Security review checklist

- Token format is opaque and secrets are hashed before persistence.
- Validation rejects malformed, unknown, wrong-secret, expired, and revoked tokens.
- Token scopes are MCP-client capabilities; there is no user-scope authorization model.
- Empty or unknown scopes fail closed; token management requires `tokens:manage` for MCP callers.
- Application use cases enforce authorization independently of MCP transport checks.
- Repository reads and writes require the authenticated owner data scope.
- API metadata responses omit the raw token and secret hash.
- Authentication failures and error mappings do not expose token material or authorization headers.
