# Phase 4 MCP token security and delivery test plan

| Requirement | Evidence |
|---|---|
| Issuing validates identity, ownership, scope normalization, and least privilege | `McpAccessTokenServiceTests` |
| Raw secret is returned only on creation and never persisted/listed | `McpAccessTokenServiceTests`; `McpAccessTokensControllerTests` |
| Token generation and validation are opaque and secure | `McpAccessTokenValidatorTests` |
| Malformed, wrong-secret, expired, and revoked tokens fail | `McpAccessTokenValidatorTests` |
| Persisted MCP client scopes drive identity | `McpAccessTokenValidatorTests` |
| Normal application access tokens remain compatible | `AccessTokenAuthenticationCompatibilityTests` |
| MCP tool read/write authorization fails before mutation | `KnowledgeToolsAuthorizationTests`; `KnowledgeTools` scope mappings |
| MCP token management has explicit authorization | `McpAccessTokenServiceTests.TokenManagement_RequiresExplicitScopeForMcpCaller` |
| API maps creation and authorization outcomes safely | `McpAccessTokensControllerTests` |
| Authenticated user data space is enforced in persistence | `CurrentUserDataScope`; repository owner filters; migration `024-scope-knowledge-data-to-users.sql` |
| SQL migration has valid runner format | `MigrationRunner` validation and no-`GO` review |
