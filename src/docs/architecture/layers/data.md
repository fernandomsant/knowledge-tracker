# `docs/architecture/layers/data.md`

# Data Layer

## Purpose

The Data layer implements persistence concerns for KnowledgeTracker.

It translates Application repository contracts into SQL Server operations while preserving transaction boundaries and relational integrity.

## Responsibilities

The Data layer may contain:

* Repository implementations
* SQL Server connection factories
* Transaction implementations
* Stored-procedure execution
* Persistence models
* Data mapping
* Schema migrations
* Stored-procedure scripts
* Database-specific configuration

## Repository Implementations

Repository implementations must implement interfaces declared in `KnowledgeTracker.Application`.

A repository implementation must:

* Use parameterized database operations
* Map database results explicitly
* Respect transaction boundaries
* Avoid exposing database-specific types to Application
* Return Domain or Application contract types
* Keep persistence concerns inside the Data layer

Repositories must not contain business rules that belong to Domain or application orchestration that belongs to use cases.

## Stored Procedures

Repositories must use stored procedures instead of embedding SQL statements directly in C#.

Do not place raw SQL queries inside repository implementations.

Repository code should only:

1. Select the stored procedure to execute.
2. Provide its parameters.
3. Execute it through the configured database connection.
4. Map the returned result.
5. Propagate the active transaction when applicable.

Example:

```csharp
await connection.QuerySingleAsync<TicketRecord>(
    "ticketing.CreateTicket",
    parameters,
    transaction,
    commandType: CommandType.StoredProcedure);
```

## Stored-Procedure Source Code

The source code for every stored procedure used by the application must be versioned in the repository.

Store procedure scripts alongside the related database schema:

```text
KnowledgeTracker.Data/database/
├── schemas/
└── procedures/
```

Recommended organization:

```text
KnowledgeTracker.Data/database/procedures/
├── identity/
├── access/
├── catalog/
├── ticketing/
└── workflow/
```

A repository must not call a stored procedure that has no corresponding versioned script in the project.

Changes to a procedure must be introduced through a migration or another deterministic database deployment mechanism.

## Schema Migrations

Schema migrations must be stored under:

```text
KnowledgeTracker.Data/database/schemas
```

Migrations must:

* Be incremental
* Be deterministic
* Use explicit schemas
* Define primary keys
* Define foreign keys
* Define unique constraints
* Define required indexes
* Use linking tables for many-to-many relationships
* Avoid multivalued columns
* Avoid comma-separated identifiers
* Avoid JSON as a replacement for relational modeling
* Use explicit constraint names
* Preserve previously deployed migration history

A migration may create one or more closely related tables when they belong to the same coherent relational structure.

## Relational Modeling

Use normalized relational structures by default.

Many-to-many relationships must use explicit linking tables.

Example:

```text
access.ProfileRoles
- ProfileId
- RoleId
```

The linking table must normally contain:

* Foreign keys to both related tables
* A composite primary key or unique constraint
* Supporting indexes where necessary

Do not create columns such as:

```text
RoleIds
CategoryIds
PermissionList
GroupIdsCsv
```

## Dependency Rules

The Data layer may depend on:

* `KnowledgeTracker.Application`
* `KnowledgeTracker.Domain`

It must not depend on:

* `KnowledgeTracker.Web`
* Controllers
* HTTP request or response types

## Prohibited Practices

Do not:

* Embed SQL strings in repository classes
* Introduce a stored procedure without versioning its source code
* Place business rules inside stored procedures without explicit architectural justification
* Return database records directly through Web endpoints
* Bypass repository contracts
* Open unrelated transactions inside individual repository methods
* Store multiple relational values in a single column

---