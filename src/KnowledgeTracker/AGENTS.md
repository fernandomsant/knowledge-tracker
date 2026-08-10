## Project Context

Knowledge Tracker is a study management platform that helps users organize knowledge, record learning progress, identify review needs, and reinforce understanding through notes, performance insights, and AI-generated questions. Its experience is centered on an interactive visual map of related subjects, with optional social and community features for sharing progress and learning collaboratively.

### Direct File Editing

Edit project files directly. Do not generate or present patches, diffs, or patch instructions unless explicitly requested. Apply all required changes to the actual files in the repository.

### Project Structure

Organize the codebase into the following projects:

#### Database Migrations

Store all database migrations inside the Data project under the `migrations/` directory.

Whenever development introduces a persisted entity, relationship, constraint, index, or structural database change, define it through explicit SQL migration files. Do not create or modify the database schema exclusively through application code.

Use one migration file per coherent schema change and name files with a sequential prefix and a descriptive name, for example:

```text
Data/
  migrations/
    001-create-users.sql
    002-create-auth-accounts.sql
    003-create-user-auth-account-relationship.sql
```

Migration files must:

* Contain deterministic SQL.
* Create all required tables, columns, keys, constraints, relationships, and indexes.
* Use explicit data types and nullability.
* Define foreign-key behavior intentionally.
* Be ordered so that dependencies are created before objects that reference them.
* Remain immutable after they have been applied; subsequent changes must be introduced through new migration files.

Every persisted Domain entity and every relationship introduced during development must have a corresponding SQL definition in `Data/migrations/`.


#### Domain

Contains the core business model and business rules.

* Entities
* Value objects
* Domain services
* Domain exceptions
* Domain enums

The Domain project must not depend on Application, Infrastructure, Data, or Web.

#### Application

Contains application behavior and abstractions.

* Use case interfaces and implementations
* Repository interfaces
* Application service interfaces
* Request and response contracts
* Application-level validation

Group use cases, repository interfaces, and contracts by functional scope or module.

The Application project may depend on Domain, but must not depend on Infrastructure, Data, or Web.

#### Infrastructure

Contains implementations for external systems and technical services that are not part of the database layer.

Examples include:

* Authentication and token services
* Email services
* File storage
* External API clients
* System clock and environment services

Infrastructure may implement interfaces defined by Application.

#### Data

Contains the SQL-based database implementation.

* SQL scripts and schemas
* Database connection management
* Repository implementations
* Query and command execution
* Row mapping
* Transaction management

Repository implementations must implement interfaces defined in Application. Keep SQL queries explicit and grouped by their functional scope.

#### Web

Contains the controller-based HTTP API.

* Controllers
* HTTP request and response models
* Middleware
* Filters
* Authentication and authorization configuration
* Dependency injection and application startup

Controllers must remain thin. They should validate HTTP-level input, call Application use cases, and translate results into HTTP responses. Business logic and direct SQL access must not be implemented in controllers.

### Folder Organization

Within each project, separate classes by responsibility and functional scope. Use folders such as `Entities`, `Repositories`, `UseCases`, `Contracts`, `Services`, `Controllers`, and `Database` where applicable.
Try to maintain UseCase classes comprehensive, do not create an unique big fat file.

Prefer scope-first organization when a module contains several related classes. For example:

```text
Application/
  Users/
    UseCases/
    Repositories/
    Contracts/

Data/
  Users/
    Repositories/
    Queries/

Web/
  Users/
    Controllers/
    Models/
```

Do not place unrelated classes in generic folders or combine multiple architectural responsibilities in the same class.


### Direct File Editing

Edit project files directly. Do not generate or present patches, diffs, or patch instructions unless explicitly requested. Apply all required changes to the actual files in the repository.

### Project Folder Organization

Organize classes into folders according to their responsibility, such as `Repositories`, `UseCases`, `Services`, `Entities`, `Interfaces`, and `Contracts`. Within these folders, group classes by their functional scope or module. Do not place unrelated classes in the same folder or keep multiple responsibilities in generic folders.

### Project File Access

Do not request user approval to read or edit files inside this project workspace. The agent already has that permission and must proceed directly; ask only for actions outside the workspace or for external, destructive, or otherwise separately authorized operations.

### Code Comments

Add comments only when they provide context that the code cannot express clearly by itself.

Comments should explain:

* Non-obvious business rules.
* Important architectural decisions.
* Complex algorithms or control flow.
* Security-sensitive behavior.
* Workarounds and their underlying reasons.
* Assumptions that future changes could invalidate.

Do not comment obvious statements, repeat method or property names, or describe each line of code. Prefer clear naming, small methods, and well-structured classes over explanatory comments.

Keep comments concise and update or remove them whenever the related implementation changes.
