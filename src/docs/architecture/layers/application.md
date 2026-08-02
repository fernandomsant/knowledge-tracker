# `docs/architecture/layers/application.md`

# Application Layer

## Purpose

The Application layer defines and coordinates the behaviors exposed by KnowledgeTracker.

It orchestrates Domain objects and persistence abstractions without implementing infrastructure or database details.

## Responsibilities

The Application layer may contain:

* Use-case contracts
* Use-case implementations
* Use-case requests and results
* Repository interfaces
* Transaction abstractions
* Authorization abstractions
* Application-level validation
* Application service contracts
* Coordination between Domain objects and repositories

## Use Case Pattern

Every application behavior must be represented by an explicit use case.

Examples include:

* Create a ticket
* Assign an analyst
* Approve a request
* Revoke a session
* Create a category

Each use case must have an interface before its implementation is created.

Example:

```text
ICreateTicketUseCase
CreateTicket
CreateTicketRequest
CreateTicketResult
```

The interface defines the application contract. The implementation coordinates the behavior.

A use case should:

1. Validate application-level preconditions.
2. Verify authorization when required.
3. Load the necessary Domain state.
4. Invoke Domain behavior.
5. Persist the resulting changes.
6. Control the transaction when multiple operations must be atomic.
7. Return an explicit result.

Use cases must remain focused on one application behavior.

## Repository Pattern

All persistence access required by Application must be represented by repository interfaces.

Repository interfaces must be created before their implementations.

Repository interfaces belong to the Application layer. Their implementations belong to the Data layer.

Repositories should be specific to the behavior or aggregate they support.

Prefer:

```text
ITicketRepository
ISessionRepository
ICategoryRepository
```

Avoid generic abstractions such as:

```text
IRepository<TEntity>
IGenericRepository
```

Repository contracts must describe domain or application intentions rather than database operations.

Prefer:

```text
GetActiveSessionAsync
AddTicketAsync
AssignAnalystAsync
```

Avoid exposing low-level persistence details such as:

```text
ExecuteSqlAsync
GetTableAsync
RunStoredProcedureAsync
```

## Dependency Rules

The Application layer may depend on:

* `KnowledgeTracker.Domain`

It must not depend directly on:

* `KnowledgeTracker.Data`
* `KnowledgeTracker.Infrastructure`
* `KnowledgeTracker.Web`
* SQL Server libraries
* ASP.NET Core controllers
* External-service SDK implementations

Infrastructure and Data dependencies must be represented through interfaces.

## Transactions

Use cases control transaction boundaries when a behavior performs multiple persistence operations that must succeed or fail together.

Repositories must participate in the transaction provided by the Application workflow rather than independently creating unrelated transactions.

Read-only use cases should not create write transactions.

## Results and Failures

Expected outcomes should be represented explicitly through result objects or well-defined application errors.

Expected business failures should not normally be represented by unhandled exceptions.

Exceptions should be reserved for unexpected technical or programming failures.

## Prohibited Practices

Do not:

* Place SQL or stored-procedure names in use cases
* Reference repository implementations
* Place HTTP concerns in Application
* Return ASP.NET Core response types
* Implement business invariants only in use cases when they belong to Domain
* Let controllers coordinate repositories directly
* Create a use-case implementation before defining its interface
* Create a repository implementation before defining its interface

---