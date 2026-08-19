# Subject note ownership test plan

1. Add Application tests for creating a note on a leaf, rejecting a parent, and rejecting reparenting under a subject with direct notes.
2. Add domain/repository-facing tests for the direct-vs-recursive query contract and note owner preservation.
3. Add migration SQL assertions through static inspection tests or focused migration validation where the existing test infrastructure permits.
4. Add frontend-level pure helper coverage if a test runner is introduced; otherwise validate the derived hierarchy index and build output.
5. Run the focused .NET test project and frontend production build, then review each assertion against the acceptance checklist.
