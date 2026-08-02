# `docs/architecture/layers/domain.md`

# Domain Layer

## Purpose

The Domain layer represents the business concepts, rules, invariants, and behavior of KnowledgeTracker.

It must remain independent from persistence, HTTP, external services, frameworks, and application orchestration.

## Responsibilities

The Domain layer may contain:

* Entities
* Value Objects
* Domain Services
* Domain Events
* Domain-specific enumerations
* Business-rule validation
* State transitions
* Invariants that must always remain valid

Business behavior should remain close to the entities and value objects that own it.

Avoid creating entities that are only passive collections of properties when they are responsible for meaningful business rules or state transitions.

## Dependency Rules

The Domain layer must not depend on:

* `KnowledgeTracker.Application`
* `KnowledgeTracker.Data`
* `KnowledgeTracker.Infrastructure`
* `KnowledgeTracker.Web`
* SQL Server or database libraries
* ASP.NET Core
* External-service SDKs

Domain types must not contain persistence, serialization, HTTP, logging, or infrastructure concerns.

## Entity and Database Correspondence

Every persisted Domain entity must have a corresponding relational representation under:

```text
KnowledgeTracker.Data/database/schemas
```

Before creating or changing the data structure of an entity, inspect the related schema migrations and tables in that directory.

Use the relational schema as the structural basis for:

* Persisted identifiers
* Required and optional properties
* Relationships
* Cardinality
* Unique constraints
* Referential integrity
* Supported persisted states

The Domain model and database schema must remain consistent.

This correspondence must not introduce persistence concerns into the Domain layer. Domain types must not contain table names, column names, SQL attributes, stored-procedure names, or database-specific logic.

The database defines the persisted structure and integrity constraints. The Domain defines the business meaning and behavior associated with that structure.

## DDD Guidelines

Entities must:

* Have a stable identity
* Protect their invariants
* Expose behavior through meaningful methods
* Avoid unrestricted state mutation
* Prevent invalid state transitions

Value Objects must:

* Be defined by their values rather than identity
* Be immutable
* Validate themselves during creation
* Represent domain concepts instead of primitive technical values

Aggregates must:

* Define a clear consistency boundary
* Expose a single aggregate root
* Protect invariants inside that boundary
* Avoid loading unrelated object graphs

Domain Services should be used only when business behavior does not naturally belong to a single entity or value object.

## Prohibited Practices

Do not:

* Reference repositories from Domain entities
* Execute queries or stored procedures
* Return HTTP responses
* Read application configuration
* Depend on controllers or use cases
* Expose public property setters without justification
* Place application orchestration inside entities
* Mirror database tables without modeling their business meaning

---