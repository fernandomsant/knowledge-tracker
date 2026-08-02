# `docs/architecture/layers/web.md`

# Web Layer

## Purpose

The Web layer exposes KnowledgeTracker behaviors through HTTP.

It is responsible for transport concerns and delegates application behavior to use cases.

## Controllers

KnowledgeTracker uses ASP.NET Core controllers.

Controllers must remain thin.

A controller may:

* Receive HTTP requests
* Apply model binding
* Validate transport-level input
* Read authenticated-user information
* Call one application use case
* Map application results to HTTP responses
* Declare routes, status codes, and authorization requirements

A controller must not:

* Access repositories directly
* Execute stored procedures
* Contain SQL
* Implement business rules
* Coordinate multiple persistence operations
* Modify Domain entities directly
* Control database transactions
* Depend on Data implementations

The expected direction is:

```text
Controller
    → Use Case Interface
        → Use Case Implementation
            → Repository Interface
                → Repository Implementation
                    → Stored Procedure
```

## HTTP Contracts

HTTP request and response models belong to the Web boundary.

Controllers must map HTTP models to Application requests and Application results to HTTP responses.

Do not expose:

* Domain entities
* Persistence records
* Stored-procedure result models
* Infrastructure SDK objects

HTTP contracts must remain explicit and stable.

## Error Mapping

Expected Application failures must be mapped consistently to HTTP status codes.

Examples:

* Invalid input → `400 Bad Request`
* Unauthenticated user → `401 Unauthorized`
* Insufficient permission → `403 Forbidden`
* Resource not found → `404 Not Found`
* Conflict with current state → `409 Conflict`

Unexpected failures must be handled by centralized exception handling rather than repeated controller-level `try/catch` blocks.

## Authentication and Authorization

Authentication identifies the caller.

Authorization determines whether the caller may execute the requested use case.

Controllers may declare authorization requirements, but detailed authorization decisions should be represented through Application contracts and use-case behavior when they depend on business context.

## Dependency Injection

The Web layer is the application composition root.

It may register:

* Use-case implementations
* Repository implementations
* Infrastructure adapters
* Configuration
* Authentication
* Authorization
* Observability
* Database connections

Concrete Data and Infrastructure implementations should only be connected to their interfaces during application composition.

## Dependency Rules

The Web layer may depend on Application contracts and the composition mechanisms required to register concrete implementations.

Business behavior must still be accessed through Application use-case interfaces.

## Prohibited Practices

Do not:

* Inject repositories into controllers
* Return Domain entities directly
* Put SQL in controllers
* Implement workflow orchestration in controllers
* Use static service locators
* Duplicate business validation already owned by Domain or Application
* Couple Application contracts to ASP.NET Core types
