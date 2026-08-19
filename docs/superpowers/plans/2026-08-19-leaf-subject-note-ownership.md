# Leaf Subject Note Ownership Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enforce leaf-only direct StudyNote ownership, expose recursive descendant notes, and update the graph UI without changing goal or connection semantics.

**Architecture:** Keep the existing direct note repository method intact and add a recursive query contract backed by a SQL recursive CTE. Application performs leaf and hierarchy validation; SQL triggers enforce the same invariant for out-of-band writes. The frontend consumes note ownership IDs to aggregate parent displays while preserving each note’s leaf owner.

**Tech Stack:** .NET 10/C#, SQL Server migration scripts, xUnit, React 18, Vite.

## Global Constraints

- Only leaf Subjects may directly own StudyNotes.
- Existing direct note queries remain direct-only for goal calculations.
- Recursive graph reads must not duplicate note relationships.
- SubjectConnections are unrelated to ParentSubjectId.
- Conflicting direct notes on non-leaf Subjects are deleted transactionally by migration.
- SQL triggers, not ordinary CHECK constraints, enforce cross-row leaf ownership.

---

### Task 1: Domain and application contracts

**Files:**
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Application/Knowledge/Repositories/ISubjectRepository.cs`
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Application/Knowledge/Repositories/IStudyNoteRepository.cs`
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Application/Knowledge/UseCases/StudyNoteService.cs`
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Application/Knowledge/UseCases/SubjectService.cs`
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Application/Knowledge/UseCases/ISubjectService.cs`
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Application/Knowledge/UseCases/IStudyNoteService.cs`
- Create: `src/KnowledgeTracker/KnowledgeTracker.Application/Knowledge/Contracts/SubjectNoteDetails.cs`
- Test: `src/KnowledgeTracker/KnowledgeTracker.Tests/Knowledge/LeafSubjectOwnershipTests.cs`

**Interfaces:**
- Add `Task<bool> HasChildrenAsync(Guid subjectId, CancellationToken ct)` to `ISubjectRepository`.
- Add `Task<IReadOnlyCollection<StudyNote>> ListBySubjectTreeAsync(Guid subjectId, CancellationToken ct)` to `IStudyNoteRepository`.
- Add `ListDescendantsAsync(Guid subjectId, CancellationToken ct)` to `IStudyNoteService` and route graph reads through it.

- [ ] **Step 1: Write failing tests** for parent note creation, direct-query preservation, recursive-query mapping, and reparenting a note-owning leaf into a parent role.
- [ ] **Step 2: Run the focused tests** and verify they fail because the contracts and checks do not exist.
- [ ] **Step 3: Implement minimal contracts and Application validation**. Note creation calls `HasChildrenAsync` and throws an `ArgumentException`; Subject create/update calls the same child check before allowing a hierarchy change that would create a parent with direct notes.
- [ ] **Step 4: Run focused tests** and verify green.

### Task 2: SQL repositories and migration

**Files:**
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Data/Knowledge/Repositories/SqlServerSubjectRepository.cs`
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Data/Knowledge/Repositories/SqlServerStudyNoteRepository.cs`
- Create: `src/KnowledgeTracker/KnowledgeTracker.Data/migrations/021-enforce-leaf-subject-note-ownership.sql`

**Interfaces:**
- Implement `HasChildrenAsync` with an indexed `EXISTS` query.
- Implement `ListBySubjectTreeAsync` with a recursive CTE rooted at the requested Subject and one note row per persisted note.

- [ ] **Step 1: Add repository tests/fixtures** for the direct and recursive query shape.
- [ ] **Step 2: Add migration SQL** that deletes dependent `StudyNoteMetrics` and then direct `StudyNotes` for Subjects with children in one transaction, then creates `AFTER INSERT, UPDATE` triggers for note writes and `AFTER INSERT, UPDATE` trigger for hierarchy writes. The triggers reject a note whose `SubjectId` has children and reject a Subject row with children plus direct notes.
- [ ] **Step 3: Implement repository methods** using the project’s explicit SQL/DbConnection conventions and existing row mapper.
- [ ] **Step 4: Verify migration has no `GO`, declares foreign-key/index behavior, and preserves topics and leaf notes.**

### Task 3: Web API and graph loading

**Files:**
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Application/Knowledge/UseCases/SubjectService.cs`
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Web/Knowledge/Controllers/StudyNotesController.cs`
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Web/Knowledge/Contracts/KnowledgeResponses.cs`
- Modify: `src/KnowledgeTracker/KnowledgeTracker.Web/Knowledge/Mappings/KnowledgeResponseMapper.cs`
- Modify: `src/frontend/knowledge/api/knowledgeClient.js`
- Modify: `src/frontend/hooks/useKnowledgeStore.js`

- [ ] **Step 1: Add a recursive subject-note response contract** retaining `StudyNote.SubjectId` as the owning leaf ID.
- [ ] **Step 2: Add a separate `GET /api/subjects/{id}/notes/recursive` endpoint** and leave the existing route direct-only.
- [ ] **Step 3: Load recursive notes for graph state** while leaving goal endpoints and direct note route unchanged.
- [ ] **Step 4: Run API/application tests** for route semantics and response ownership.

### Task 4: Frontend behavior

**Files:**
- Modify: `src/frontend/hooks/useKnowledgeStore.js`
- Modify: `src/frontend/components/KnowledgeGraph.jsx`
- Modify: `src/frontend/App.jsx`
- Test: `src/frontend` existing test location or add a focused utility test if configured.

- [ ] **Step 1: Derive descendant note lists** from the hierarchy, preserving each note’s `subjectId` as the leaf owner and avoiding duplicate IDs.
- [ ] **Step 2: Disable the Add note action** and change empty-state copy for parents; keep editing/deleting leaf notes available from aggregated views.
- [ ] **Step 3: Display the owning leaf Subject** beside each aggregated note.
- [ ] **Step 4: Build the frontend** and run configured tests/lint.

### Task 5: Verification and scope review

**Files:**
- All changed files above.

- [ ] **Step 1: Run `dotnet test` using the project’s test runner guidance.**
- [ ] **Step 2: Run `npm run build` for the frontend.**
- [ ] **Step 3: Run GitNexus `detect_changes({scope: "all"})` and confirm only intended symbols/flows are affected.**
- [ ] **Step 4: Review migration with `rg` for forbidden `GO` separators and report the direct-vs-recursive query boundary.**
