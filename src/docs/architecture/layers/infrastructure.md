# `docs/architecture/layers/infrastructure.md`

# Infrastructure Layer

## Purpose

The Infrastructure layer implements technical capabilities required by the Application layer that are not part of relational persistence.

It integrates KnowledgeTracker with operating-system services, security mechanisms, external systems, and cross-cutting technical resources.

## Responsibilities

The Infrastructure layer may contain implementations for:

* Password hashing
* Token issuance and validation
* File storage
* Email delivery
* Notifications
* External APIs
* Message brokers
* System clock
* Cryptographic generators
* Distributed locks
* External identity providers
* Telemetry exporters
* Environment-specific technical services

## Interface Ownership

Interfaces required by Application must be declared in `KnowledgeTracker.Application`.

Infrastructure provides their concrete implementations.

Example:

```text
KnowledgeTracker.Application:
IAccessTokenIssuer

KnowledgeTracker.Infrastructure:
AccessTokenIssuer
```

The implementation must not leak infrastructure-specific types into Application or Domain.

## Dependency Rules

The Infrastructure layer may depend on:

* `KnowledgeTracker.Application`
* `KnowledgeTracker.Domain`

It must not depend on:

* `KnowledgeTracker.Web`
* Controllers
* HTTP response types
* Data repository implementations, unless an explicitly documented integration requires it

Infrastructure must not be referenced by Domain.

## Configuration

Infrastructure services may consume configuration through typed options or equivalent configuration abstractions.

Secrets must not be committed to the repository.

Environment-specific values must come from secure configuration sources.

## External Integrations

External integrations must be isolated behind interfaces.

Implementations must:

* Define timeouts
* Propagate cancellation
* Handle transient failures appropriately
* Avoid logging secrets
* Map external contracts into internal contracts
* Keep external SDK types out of Domain and Application

## Observability

Infrastructure implementations should provide structured logs, metrics, and traces where technically relevant.

Logging must not contain:

* Passwords
* Access tokens
* Refresh tokens
* Secret keys
* Sensitive personal information
* Complete external payloads unless explicitly authorized

## Prohibited Practices

Do not:

* Implement business rules in infrastructure adapters
* Reference Infrastructure from Domain
* Expose external SDK objects through Application contracts
* Read configuration directly throughout the codebase
* Hardcode credentials or environment-specific addresses
* Treat infrastructure failures as domain decisions

---