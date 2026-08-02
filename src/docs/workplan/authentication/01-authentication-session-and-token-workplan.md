# Authentication Session and Token Workplan

## Status

Blocked pending the decisions and project structures listed in the module
specification at `docs/modules/authentication/authentication.md`.

## Module Reference

`docs/modules/authentication/authentication.md`

## Goal

Implement backend authentication that creates a session for a newly observed
user agent, issues signed access and opaque refresh tokens, and atomically
rotates refresh tokens with session-wide replay detection.

## Affected Layers

* Domain: session state and refresh-sequence invariants.
* Application: authentication, access-token validation, refresh, and revocation
  use cases plus repository and technical-service interfaces.
* Data: session and opaque-refresh-token persistence plus versioned stored
  procedures that perform atomic rotation or revocation.
* Infrastructure: credentials verifier, clock, cryptographic nonce and refresh
  secret generation, and signing and validation of backend tokens.
* Web: thin endpoints that map requests to the use cases and obtain user-agent,
  client IP address, and source port as transport metadata.

## Incremental Implementation Stages

### Stage 1 — Establish authentication contracts and domain rules

1. Locate the existing User aggregate and credential-verification behavior.
2. Define application contracts before implementations:
   `IAuthenticateUserUseCase`, `IRefreshSessionUseCase`,
   `IValidateAccessTokenUseCase`, `IRevokeSessionUseCase`,
   `IAuthenticationSessionRepository`, `ICredentialVerifier`,
   `ITokenIssuer`, `ITokenValidator`, `IClock`, and secure-random abstractions.
3. Model a session aggregate that owns active, revoked, and expired states and
   the latest issued refresh-token sequence number.
4. Model the invariant that only a refresh token whose sequence equals the
   current session sequence may be rotated.

### Stage 2 — Create the session and opaque-refresh-token schema

This stage is a migration workplan. It must be completed and marked `Ready for
implementation` before SQL is written.

#### Required relational structures

`access.AuthenticationSessions` must contain, at minimum:

| Column | Type | Nullability | Rule |
| --- | --- | --- | --- |
| `AuthenticationSessionId` | `uniqueidentifier` | No | Primary key. |
| `UserId` | `<must match existing User key>` | No | Foreign key to the existing user table. |
| `Nonce` | `uniqueidentifier` or `varbinary(32)` | No | Unique cryptographic session nonce. |
| `UserAgent` | `<bounded Unicode text type>` | No | Original or normalized user-agent value, as decided by the module. |
| `ClientIpAddress` | `varchar(45)` | No | Canonical IPv4 or IPv6 textual address. |
| `ClientSourcePort` | `int` | No | Range-constrained to `0` through `65535`. |
| `AuthenticatedAtUtc` | `datetime2` | No | Source for `auth_time`. |
| `ExpiresAtUtc` | `datetime2` | No | Session expiration. |
| `LatestRefreshSequenceNumber` | `bigint` | No | Begins at `1` and increases only atomically. |
| `RevokedAtUtc` | `datetime2` | Yes | Non-null means the session cannot refresh. |
| `CreatedAtUtc` | `datetime2` | No | Audit timestamp. |
| `UpdatedAtUtc` | `datetime2` | No | State-change timestamp. |

`access.OpaqueRefreshTokens` must contain, at minimum:

| Column | Type | Nullability | Rule |
| --- | --- | --- | --- |
| `OpaqueRefreshTokenId` | `uniqueidentifier` | No | Primary key. |
| `AuthenticationSessionId` | `uniqueidentifier` | No | Foreign key to `access.AuthenticationSessions`. |
| `TokenHash` | `varbinary(64)` | No | Unique one-way hash of the client token secret. |
| `SequenceNumber` | `bigint` | No | Backend-only issued sequence number. |
| `ExpiresAtUtc` | `datetime2` | No | Must not exceed session expiration. |
| `ConsumedAtUtc` | `datetime2` | Yes | Set by successful rotation. |
| `RevokedAtUtc` | `datetime2` | Yes | Set when invalidated without successful rotation. |
| `CreatedAtUtc` | `datetime2` | No | Audit timestamp. |

#### Required relationships and constraints

* `AuthenticationSessions.UserId` references the existing User table with
  `RESTRICT` deletion behavior until the user-retention policy is specified.
* `OpaqueRefreshTokens.AuthenticationSessionId` references
  `AuthenticationSessions.AuthenticationSessionId` with `CASCADE` deletion only
  if sessions are physically deleted; otherwise sessions and tokens must be
  retained and revoked together.
* `TokenHash` has a unique constraint.
* `(AuthenticationSessionId, SequenceNumber)` has a unique constraint.
* `LatestRefreshSequenceNumber >= 1` and `ClientSourcePort BETWEEN 0 AND 65535`
  are check constraints.
* An index supports lookup by `TokenHash` and an index supports active-session
  lookup by `(UserId, UserAgent, ExpiresAtUtc)`.

#### Atomic stored-procedure behavior

Create versioned procedures that, inside one transaction:

1. Create a session with sequence `1` and its hashed opaque refresh token.
2. Resolve a refresh token hash, lock its session, compare its sequence with
   `LatestRefreshSequenceNumber`, consume it, increment the sequence, and store
   the next hashed token when the comparison succeeds.
3. Revoke the session and all active refresh tokens when the comparison fails.

The rotation procedure must return a distinct replay outcome so the Application
layer can reject the request without exposing token metadata.

### Stage 3 — Implement technical token services

1. Generate cryptographic nonces and opaque refresh-token secrets.
2. Sign access tokens with the configured backend key and required claims.
3. Issue refresh-token secrets only as opaque client values while retaining their
   hash and sequence metadata server-side.
4. Validate token signature, issuer, expiration, required claims, and token type.

### Stage 4 — Implement use cases and repositories

1. Authenticate credentials, determine whether the user agent is new, create
   the session when required, and issue a token pair.
2. Call the rotation procedure for refreshes and issue a replacement pair only
   after its atomic success outcome.
3. Map a replay outcome to session revocation and an unauthenticated result.
4. Implement explicit session-revocation behavior.

### Stage 5 — Expose and verify the HTTP boundary

1. Add thin authentication, refresh, and revocation endpoints that call one use
   case each.
2. Map invalid credentials and invalid or expired tokens to `401`; map a replay
   to `401` without disclosing whether the token was previously used.
3. Test initial issue, valid rotation, concurrent refresh, replay after rotation,
   session revocation, expiry, invalid signature, invalid issuer, and the
   required claim set.

## Blocking Decisions

* The repository contains no User table or credential-validation implementation,
  so the `UserId` foreign-key target and authentication mechanism cannot be
  defined safely.
* The repository contains no Data project, schema-migration folder, or stored
  procedure convention to receive the required SQL implementation.
* The repository contains no Web project or HTTP conventions for obtaining and
  trusting client IP address and source port, particularly when reverse proxies
  are in use.
* Signing configuration and token lifetimes are security decisions that must be
  supplied by secure configuration and approved policy.

## Completion Evidence

The implementation is complete only when each stage has a corresponding progress
record under `docs/progress/authentication/`, all migrations and stored
procedures are versioned, and automated tests demonstrate atomic rotation and
session-wide replay revocation.
