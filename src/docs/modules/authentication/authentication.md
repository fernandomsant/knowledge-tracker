# Authentication Module

## Status

Draft. The behavior defined here is ready for planning, but implementation is
blocked until the hosting projects and the existing user and credential model
are available.

## Objective

Authenticate a user with valid credentials, create a backend session for a new
user agent, issue backend-signed access and refresh tokens, and safely refresh
tokens through rotation and replay detection.

## Scope

The module must provide these application behaviors:

1. Authenticate credentials and issue an access-token and refresh-token pair.
2. Validate backend-signed access tokens.
3. Refresh a valid refresh token by rotating it atomically.
4. Revoke the complete session immediately when a refresh-token replay is
   detected.

## Token Requirements

Both access and refresh tokens must be backend-signed and must carry these
claims:

| Claim | Meaning |
| --- | --- |
| `iss` | Configured token issuer. |
| `sub` | Stable identifier of the authenticated user. |
| `auth_time` | UTC instant at which the user successfully authenticated. |
| `exp` | UTC expiration instant for the token. |
| `nonce` | Cryptographically random session nonce. |

The access token is a signed token whose claims are validated by the backend.

The refresh token exposed to the client must be opaque. Its sequence number,
session identifier, and any other server metadata must not be readable by the
client and must not appear in a JWT claim or other client-readable payload. The
client receives only an unguessable refresh-token secret. The backend stores a
one-way hash of that secret together with the metadata required to resolve it.

## Session Requirements

A session must be created whenever a user successfully authenticates from a
new user agent. A session stores:

* The authenticated user.
* Its expiration time.
* A cryptographically random nonce.
* User-agent information.
* The client IP address.
* The client source port.
* The sequence number of the latest refresh token issued.
* Its active, revoked, or expired state.

The initial refresh token for a new session has sequence number `1`. Each
successful refresh issues the next sequential number. The sequence number is
backend-only metadata.

## Refresh Rotation and Replay Detection

Each opaque refresh token resolves to a session and an issued sequence number.
The refresh operation must execute in one database transaction or an equivalent
atomic compare-and-swap operation:

1. Resolve the opaque token only through its one-way hash.
2. Verify that the session is active and unexpired.
3. Compare the token's stored sequence number with the session's current
   sequence number.
4. When they match, invalidate the used token, increment the session sequence
   number, persist the replacement opaque token and its metadata, and issue a
   replacement token pair atomically.
5. When they do not match, revoke the entire session immediately and reject the
   refresh request.

No partial result may leave a valid refresh token without the corresponding
session sequence state, or vice versa.

## Security Rules

* Never log credentials, access tokens, refresh tokens, token signing keys, or
  unredacted authorization headers.
* Token signing keys and token lifetimes must come from secure configuration,
  not source control.
* Token validation must verify the signature, issuer, expiration, required
  claims, and token type before accepting the token.
* Refresh-token secrets must be generated with a cryptographically secure random
  generator and stored only as one-way hashes.
* Session revocation must prevent all subsequent refreshes for that session.

## Decisions Required Before Implementation

1. Which existing User aggregate/table supplies the stable user identifier and
   which use case validates credentials?
2. What are the configured access-token lifetime, refresh-token lifetime,
   issuer value, signing algorithm, and key-rotation policy?
3. Is a "new user agent" determined by exact normalized user-agent text, a
   server-side fingerprint, or another documented identity rule?
4. What HTTP routes and transport conventions expose authentication, refresh,
   and session revocation?
5. Which .NET projects, database connection conventions, and migration folders
   will host the Domain, Application, Data, Infrastructure, and Web code?
