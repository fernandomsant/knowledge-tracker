# Subject note ownership test review

## Review result

- `Create_rejects_parent_subjects_and_allows_leaf_subjects` verifies Application note creation enforces leaf-only ownership and preserves the successful leaf path.
- `Reparenting_rejects_a_subject_with_direct_notes_as_the_new_parent` verifies Application hierarchy validation rejects a new parent with direct notes.
- `Subject_details_use_recursive_notes_without_changing_direct_note_queries` verifies recursive subject/application reads expose a descendant note once while the existing direct repository query remains empty for the parent.
- SQL migration execution passed through `npm run migrate`, including the conflict audit/backfill and both database triggers.
- `dotnet test src/KnowledgeTracker/KnowledgeTracker.Tests/KnowledgeTracker.Tests.csproj --no-restore` passed 6/6 tests.
- `dotnet build src/KnowledgeTracker/KnowledgeTracker.Web/KnowledgeTracker.Web.csproj --no-restore` passed with 0 warnings and 0 errors.
- `npm run build` passed for the frontend.

## Limitations

The static test-pairing analyzer could not complete because NuGet package restore failed with the environment's TLS credentials. GitNexus MCP `impact` and `detect_changes` tools were not exposed; the checked-in GitNexus CLI index was refreshed successfully and is current.
