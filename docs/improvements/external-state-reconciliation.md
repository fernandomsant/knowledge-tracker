# Browser state does not reconcile external changes

Affected file:

- `src/frontend/hooks/useKnowledgeStore.js` — loads the workspace on mount/workspace change only.

Changes made through MCP, another browser tab, or another client remain invisible until the workspace is reloaded.

Improvement suggestion: add a lightweight refetch on window focus/visibility, or introduce push invalidation when real-time consistency becomes necessary. Guard overlapping loads so an older response cannot overwrite newer state. Do not implement until requested.
