# Layout changes can be silently lost

Affected file:

- `src/frontend/App.jsx:285` — `flushLayoutSave` clears pending positions before the save result is known, and workspace switching ignores failure.

Improvement suggestion: snapshot the queue, clear entries only after a successful save, retain newer edits made while a request is in flight, and abort workspace changes when the flush fails. Do not implement until requested.
