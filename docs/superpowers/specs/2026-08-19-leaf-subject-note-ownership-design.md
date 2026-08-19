# Leaf Subject Note Ownership

## Goal

Only leaf Subjects directly own StudyNotes. Parent Subjects expose descendant notes through a separate recursive query, while existing direct-only note queries remain unchanged for goal calculations.

## Architecture

The Domain keeps Subject hierarchy semantics and does not model recursive query behavior. Application validates that note creation targets a leaf and that reparenting or child creation cannot make a note-owning Subject a parent. Application adds a distinct recursive note query contract for graph reads.

Data preserves the existing `StudyNotes.SubjectId` ownership model. A migration audits and deletes direct notes on Subjects that already have children, including their dependent metrics. SQL Server triggers enforce the leaf-only rule for note writes and hierarchy changes because a normal CHECK constraint cannot inspect related rows. Recursive graph reads use a CTE and return the persisted owning leaf SubjectId on each note.

The frontend derives direct and aggregated note views from the API's note ownership, disables note creation on parents, and labels aggregated notes with the owning leaf Subject. SubjectConnections remains independent from ParentSubjectId.

## Data flow

1. Subject summaries and details continue to load as before.
2. Graph note reads call the new recursive application/data query; goal calculations continue to call direct `ListBySubjectAsync`.
3. A create-note request is rejected by Application if the target Subject has children, before persistence.
4. A Subject create/update that would leave direct notes on a parent is rejected by Application and the database trigger.
5. The migration deletes only notes directly owned by non-leaf Subjects and their metrics; leaf-owned notes and all topics remain.

## Testing

Tests cover the Domain leaf invariant, Application rejection of parent note creation and invalid reparenting, preservation of direct query semantics, recursive query contract, migration trigger/check definitions, and frontend disabling/aggregation behavior. Existing subject, note, topic, metric, goal, and connection behavior remains covered by the current suite.
