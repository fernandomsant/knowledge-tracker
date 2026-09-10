# Phase 4 MCP token security and delivery review

## Result

- Full test suite: 46 passed.
- Focused authentication tests: 32 passed.
- Full solution build: passed with zero warnings and zero errors.
- Migration file review: sequential `024` migration, no `GO` batch separators, immutable SQL-runner compatible shape.
- LocalDB migration execution: blocked because the host cannot create/connect to the LocalDB automatic instance.

## Completed coverage

- Issuing tests cover authenticated ownership, normalized scopes, empty scopes, future expiry, one-time secret exposure, and non-persistence of raw secret material.
- Validation tests cover token parsing, unknown identifiers, wrong secrets, expiry, revocation, last-used persistence, and persisted MCP client scopes.
- Authorization tests cover MCP tool read/write checks, fail-closed behavior before application mutation, and explicit token-management scope enforcement.
- API tests cover creation response secret exposure and metadata-only list responses.
- Normal bearer-token compatibility is covered independently of MCP token validation.
- Knowledge persistence paths are owner-filtered, and the new migration adds/backfills `Subjects.UserId` with a foreign key and index.

## Remaining environment limitation

- A SQL Server/LocalDB connection is required to execute and verify migration SQL against a live database. No connection string is configured in this checkout, and the documented LocalDB instance is unavailable on the host.
- MCP scopes are client capabilities only. The authenticated user owns and issues the token; there is no user-scope authorization model.
