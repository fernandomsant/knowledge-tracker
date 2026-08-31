<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **knowledge-tracker** (390 symbols, 700 relationships, 24 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> Index stale? Run `node .gitnexus/run.cjs analyze` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? `npx gitnexus analyze` (npm 11 crash → `npm i -g gitnexus`; #1939).

## Always Do

- **MUST run GitNexus analysis when needed.** Run it whenever the index is absent, stale, or after major structural code changes before relying on GitNexus results.
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
| Understand architecture / "How does X work?" | `.agents/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.agents/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.agents/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.agents/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.agents/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.agents/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->

## GitNexus and Serena

Use **GitNexus** and **Serena** as complementary codebase-navigation and analysis tools whenever they can reduce unnecessary file reading or improve understanding of the repository.

### GitNexus

Use GitNexus primarily for **repository-wide structural and dependency analysis**.

Prefer GitNexus when you need to:

* Understand the overall architecture of the repository.
* Locate relevant modules, classes, functions, interfaces, or symbols.
* Identify dependencies and relationships between components.
* Trace how a feature or concept is distributed across the codebase.
* Determine the potential impact of a change before modifying code.
* Find likely entry points before performing more detailed inspection.

Do not manually scan large portions of the repository when GitNexus can identify the relevant code more efficiently.

### Serena

Use Serena primarily for **symbol-level inspection, navigation, and modification**.

Prefer Serena when you need to:

* Inspect a specific class, method, function, interface, or other symbol.
* Find references or usages of a symbol.
* Navigate between related definitions and implementations.
* Understand the local context surrounding code that will be modified.
* Perform precise code modifications without unnecessarily reading or rewriting entire files.

### Recommended Workflow

For non-trivial changes:

1. Use **GitNexus** to understand the affected architecture, dependencies, and relevant areas of the repository.
2. Use **Serena** to inspect the specific symbols and implementations identified during that analysis.
3. Make the required changes only after understanding their callers, dependencies, and expected impact.
4. Reuse GitNexus or Serena as necessary to verify that related code has not been overlooked.

Avoid exhaustive repository exploration when either tool can answer the question more directly. The objective is to gather enough context to make a correct change while minimizing unnecessary file reads and token usage.

### `/progress`

Use `/progress` only when an implementation is large enough to span multiple context windows.

Create:

```text id="g60zsv"
/progress/<task-name>.md
```

Keep it short and abstract. Its purpose is only to prevent the agent from losing implementation state.

Use:

```markdown id="ht74qo"
### Task

#### Goal
Brief target state.

#### Completed
- Major completed capabilities.

#### Current
Brief current implementation state.

#### Remaining
- Major remaining work.

#### Decisions
- Important decisions that must not be accidentally reversed.

#### Blockers
- Relevant unresolved issues.
```

Do not record:

* individual file edits;
* commands executed;
* detailed reasoning;
* implementation diary entries.

Update the file only after meaningful milestones or before context exhaustion.

When resuming a long task, read its `/progress` file first.

Source code and permanent architecture documentation always take precedence over `/progress`.

Delete the progress file after the implementation is complete unless it still serves an explicit project purpose.

# Basic Instructions

## Projects and `AGENTS.md`

The GitNexus configuration defined above applies to both projects under `src/`:

* `frontend/`
* `KnowledgeTracker/`

Each project contains its own `AGENTS.md` file with project-specific instructions.

Before modifying files in either project, the agent must read and follow the corresponding `AGENTS.md`. If changes affect both projects, both `AGENTS.md` files must be read.
