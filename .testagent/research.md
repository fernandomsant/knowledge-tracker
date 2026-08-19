# Subject note ownership test research

## Scope

This is a broad cross-layer change covering the Domain/Application/Data/Web contracts and repositories, SQL migrations, and the React knowledge store and SubjectDrawer.

## Existing conventions

- .NET tests use xUnit in `src/KnowledgeTracker/KnowledgeTracker.Tests` and target `net10.0`.
- Application services receive repository interfaces through primary constructors and translate domain entities to records.
- SQL Server migrations are immutable, sequential `.sql` files executed as one transaction by `MigrationRunner`.
- The frontend uses React hooks, derived `useMemo` indexes, and plain JSX/CSS; no frontend test runner is configured.

## Acceptance checklist

- Direct note repository queries remain direct; a separate recursive query returns each note once with its leaf owner.
- Existing parent/direct-note conflicts are audited and migrated transactionally, including subject-scoped topics.
- Application note creation rejects non-leaf subjects; application reparenting rejects parents that already own notes.
- SQL triggers protect note writes and hierarchy writes from invalid leaf ownership.
- Subject detail loading and the frontend aggregate descendant notes while showing the owning leaf subject.
- SubjectConnections continue to use only their own relationship columns.
- Goal calculations continue using the existing direct-note repository method.
- Tests cover the new Application behavior and recursive/direct repository contract where test seams permit.
