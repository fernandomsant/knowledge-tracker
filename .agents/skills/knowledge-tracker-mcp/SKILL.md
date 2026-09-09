---
name: knowledge-tracker-mcp
description: Use the Knowledge Tracker MCP server to understand or manage workspace subjects, topics, notes, and goals. Apply when a request concerns Knowledge Tracker data or its entity relationships; not for changing the MCP server implementation.
---

# Knowledge Tracker MCP

Use the registered Knowledge Tracker MCP tools for a user's study data. The provider prefix varies by client; use the operation names in [the entity model and operation reference](references/operations.md).

## Entity hierarchy

```text
Workspace
+-- Subject (a tree: each subject may have one parent subject)
    +-- Topic (owned directly by that subject)
    +-- Study note (only on a leaf subject; tagged with one of its topics)
    |   +-- Metric values (optional; reference pre-existing metric definitions)
    +-- Goal (for that subject and one of its topics)
        +-- Checklist sub-goals (TargetDate goals only)
```

A subject's topic IDs cannot be used by a sibling, parent, or child subject. Notes and goals must use a topic owned by the same subject they reference.

## Working rules

- Every operation needs `workspaceName`. Use the name supplied by the user; never guess one.
- Resolve IDs from the server: list subjects, then list topics for the intended subject. For note or goal work, verify the selected topic's `subjectId` matches the target subject.
- A parent subject cannot own notes. Create the note on a leaf subject; use `list_notes(..., includeDescendants: true)` to read a subtree.
- A goal may be attached to any subject. A goal on a leaf evaluates that topic's notes; a goal on a parent aggregates qualifying notes from its descendants.
- Metric definitions are not discoverable through this MCP catalog. Only send a metric-definition ID supplied by the user or returned in existing note/goal details.
- Treat `create_*` and `complete_goal` as external writes. Invoke them only when the user requested that change.
- Use ISO-8601 dates and times. Keep MCP access tokens out of prompts, source files, and tool arguments.

Read [the entity model and operation reference](references/operations.md) before constructing a note, goal, or metric request.
