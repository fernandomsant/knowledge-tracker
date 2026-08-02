<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **knowledge-tracker** (19 symbols, 17 relationships, 0 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> Index stale? Run `node .gitnexus/run.cjs analyze` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? `npx gitnexus analyze` (npm 11 crash → `npm i -g gitnexus`; #1939).

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows. For regression review, compare against the default branch: `detect_changes({scope: "compare", base_ref: "main"})`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `query({search_query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).

## Never Do

- NEVER edit a function, class, or method without first running `impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit changes without running `detect_changes()` to check affected scope.

## Resources

| Resource | Use for |
|----------|---------|
| `gitnexus://repo/knowledge-tracker/context` | Codebase overview, check index freshness |
| `gitnexus://repo/knowledge-tracker/clusters` | All functional areas |
| `gitnexus://repo/knowledge-tracker/processes` | All execution flows |
| `gitnexus://repo/knowledge-tracker/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `~/.agents/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `~/.agents/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `~/.agents/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `~/.agents/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `~/.agents/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `~/.agents/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->

## Project Context

Knowledge Tracker is a study management platform that helps users organize knowledge, record learning progress, identify review needs, and reinforce understanding through notes, performance insights, and AI-generated questions. Its experience is centered on an interactive visual map of related subjects, with optional social and community features for sharing progress and learning collaboratively.

## Module Planning and Implementation Tracking

Project features are specified, planned, and tracked through a three-stage
documentation workflow:

1. `docs/modules/<module>/` contains the module specification and all information
   required to describe what must be implemented. The agent must create or
   modify module documentation only when explicitly instructed by the user and
   must follow the user's directions for its content.
2. After the module documentation exists, `docs/workplan/<module>/` contains one
   or more work-plan files. Each file represents one incremental, executable
   stage of the module's implementation plan and must give another agent enough
   context and precise instructions to complete that stage while following the
   project's architecture and conventions. A module may use a single work-plan
   file when one stage is sufficient, or multiple ordered files when the work
   must be implemented incrementally.
3. While executing the work plan, `docs/progress/<module>/` contains one or more
   progress files organized in the same way. Each progress file corresponds to
   one work-plan stage and must state what was implemented for that stage, what
   was verified, any relevant decisions or deviations, and what remains to be
   done. A module with multiple executed work-plan stages must therefore have
   multiple corresponding progress files.

The required lifecycle is:

```text
Explicit user command and instructions
    → docs/modules/<module>/
        → docs/workplan/<module>/
            → implementation
                → docs/progress/<module>/
```

Before planning or implementing a module, read its complete documentation under
`docs/modules/<module>/`. Before implementing a stage, read its specific
work-plan file under `docs/workplan/<module>/`, all prerequisite work-plan
stages, and the existing records under `docs/progress/<module>/` so work resumes
from the actual recorded state rather than being repeated or assumed.

Work-plan and progress documentation must remain aligned: every executed
work-plan stage must have a distinct corresponding progress file. Use matching
stage identifiers or filenames so the relationship is unambiguous. Progress
must not claim completion without implementation and verification evidence.

## Layer Architecture Documentation

KnowledgeTracker is implemented using a layered architecture.

Before creating, modifying, moving, or reviewing code in a layer, read the corresponding architecture document:

| Layer                     | Required documentation                       |
| ------------------------- | -------------------------------------------- |
| `KnowledgeTracker.Domain`         | `docs/architecture/layers/domain.md`         |
| `KnowledgeTracker.Application`    | `docs/architecture/layers/application.md`    |
| `KnowledgeTracker.Data`           | `docs/architecture/layers/data.md`           |
| `KnowledgeTracker.Infrastructure` | `docs/architecture/layers/infrastructure.md` |
| `KnowledgeTracker.Web`            | `docs/architecture/layers/web.md`            |

When a task affects multiple layers, read every applicable layer document before making changes.

### Required Workflow

1. Identify every layer affected by the requested behavior.
2. Read the architecture document for each affected layer.
3. Read the business specification for the affected module or use case.
4. Inspect the existing relational schema under `KnowledgeTracker.Data/database/schemas` when creating or changing persisted Domain entities.
5. Use GitNexus to locate structurally similar implementations and related symbols.
6. Use GitNexus impact analysis before changing existing public contracts or shared symbols.
7. Implement the smallest complete behavior that satisfies the specification.
8. Verify that dependencies continue to follow the permitted direction.
9. Review the final changes against every applicable layer document.
10. Use GitNexus change detection before declaring the task complete.

### Mandatory Layer Direction

The intended dependency direction is:

```text
KnowledgeTracker.Web
    → KnowledgeTracker.Application
        → KnowledgeTracker.Domain

KnowledgeTracker.Data
    → KnowledgeTracker.Application
    → KnowledgeTracker.Domain

KnowledgeTracker.Infrastructure
    → KnowledgeTracker.Application
    → KnowledgeTracker.Domain
```

`KnowledgeTracker.Domain` must not depend on any other KnowledgeTracker project.

`KnowledgeTracker.Application` must not depend on Data, Infrastructure, or Web.

Controllers must call use-case interfaces and must not access repositories directly.

Repository interfaces and use-case interfaces must be defined before their respective implementations.

Repository implementations must execute versioned stored procedures rather than embedding SQL directly in C#.

Do not assume that a familiar architectural pattern is implemented generically. Follow the precise conventions defined in the layer documentation and the existing KnowledgeTracker codebase.

## Migration Specification Workflow

Database changes use a two-stage workflow.

### Analysis stage

The analysis agent must convert the user's request into a Migration
Specification based on:

- the business request;
- the current relational schema;
- the related Domain structures;
- the Data-layer architecture documentation;
- related code discovered through GitNexus.

The analysis agent must not create migration SQL.

Read `docs/architecture/database/migration.md` before writing the specification.

The generated specification must be stored under:

`docs/changes/migrations/<migration-id>-<name>.md`

### Implementation stage

Before creating or modifying a migration, the implementation agent must read:

1. The applicable Migration Specification.
2. `docs/architecture/layers/data.md`.
3. The referenced existing migrations and procedures.
4. The related Domain structures.

The Migration Specification is authoritative for the requested change.

The implementation agent may report contradictions or missing decisions, but
must not silently alter cardinality, nullability, ownership, uniqueness,
deletion behavior, or compatibility requirements.

### Direct File Editing

Edit project files directly. Do not generate or present patches, diffs, or patch instructions unless explicitly requested. Apply all required changes to the actual files in the repository.

### Project Folder Organization

Organize classes into folders according to their responsibility, such as `Repositories`, `UseCases`, `Services`, `Entities`, `Interfaces`, and `Contracts`. Within these folders, group classes by their functional scope or module. Do not place unrelated classes in the same folder or keep multiple responsibilities in generic folders.
