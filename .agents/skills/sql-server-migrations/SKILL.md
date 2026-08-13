---
name: sql-server-migrations
description: Create or modify SQL Server migrations for Knowledge Tracker. Use when changing persisted schema, tables, columns, constraints, indexes, or SQL data backfills under src/KnowledgeTracker/KnowledgeTracker.Data/migrations.
---

# Knowledge Tracker SQL Server Migrations

Create one immutable, sequentially numbered `.sql` file in `src/KnowledgeTracker/KnowledgeTracker.Data/migrations/` for every schema change.

## Runner contract

The runner executes each file as one SQL command inside a transaction. It rejects standalone `GO` lines. `GO` is a client-side batch separator, not SQL Server syntax accepted by `DbCommand`.

Never use `GO` in a migration.

When SQL Server requires a separate batch, execute that statement through dynamic SQL:

```sql
EXEC(N'
ALTER TABLE dbo.SubjectGoals
ADD CONSTRAINT CK_SubjectGoals_Priority CHECK (PriorityPosition > 0);
');
```

Use this only for statements that require a batch boundary, such as `CREATE OR ALTER PROCEDURE` or `CREATE VIEW`.

## Workflow

1. Inspect the latest migration number and current table definition.
2. Add `NNN-descriptive-change.sql`; never edit an applied migration.
3. Use explicit types, nullability, defaults, constraints, foreign keys, and indexes.
4. Backfill existing data before enforcing `NOT NULL`, unique, or check constraints.
5. Do not use `GO`; use `EXEC(N'...')` only when a batch boundary is required.
6. Check with `rg -n "(?im)^\s*GO\s*(?:--.*)?$" <migration-file>`.
7. Run `npm run migrate` against LocalDB.

## Required-column pattern

```sql
ALTER TABLE dbo.Example ADD SortOrder BIGINT NULL;

;WITH Ordered AS
(
    SELECT Id, ROW_NUMBER() OVER (ORDER BY CreatedAtUtc, Id) AS SortOrder
    FROM dbo.Example
)
UPDATE target SET SortOrder = Ordered.SortOrder
FROM dbo.Example target
JOIN Ordered ON Ordered.Id = target.Id;

ALTER TABLE dbo.Example ALTER COLUMN SortOrder BIGINT NOT NULL;
CREATE UNIQUE INDEX UX_Example_SortOrder ON dbo.Example (SortOrder);
```
