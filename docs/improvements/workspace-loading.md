# Workspace loading scales badly

Affected files:

- `src/frontend/knowledge/api/knowledgeClient.js:34` — loads summaries, then makes subject-detail, connection, and goal requests per subject.
- `src/KnowledgeTracker/KnowledgeTracker.Application/Knowledge/UseCases/SubjectService.cs:17` — recursively loads descendant notes and reads all layouts for each subject.

Improvement suggestion: add a workspace snapshot/bulk read path that returns subjects, notes, layouts, connections, goals, topics, and metric definitions in one API response backed by set-based repository queries. This removes the browser request fan-out and repeated database work. Do not implement until requested.
