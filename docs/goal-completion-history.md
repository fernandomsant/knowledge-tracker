# Goal completion history

Goal occurrence dates use the UTC calendar. A daily occurrence is one UTC date, a weekly occurrence runs Monday through Sunday, and a monthly occurrence runs from the first through the last UTC date of the month. Custom goals have one configured occurrence. All-time goals have one occurrence from their creation date through their deactivation date (or the current UTC date while active). The API returns the full occurrence window even when a requested range only partially overlaps it.

`CompletedAtUtc` is the UTC instant at which the backend registered completion. An occurrence is met only when a row exists in `SubjectGoalCompletions`; missing rows are unmet. Registration is idempotent through the database unique key.

Daily, weekly, and monthly goals remain active after a completion. Manual completion and sub-goal completion register the current occurrence; one-time custom/all-time completion goals also retain their existing permanent `IsCompleted` state. Metric occurrences are recalculated after note create, update, and delete. If a correction lowers a metric below its target, the corresponding completion row is removed, so the history reflects the current authoritative notes.

The migration backfills deterministic metric completion history from persisted study notes and preserves existing one-time completion timestamps. Recurring manual history cannot be recovered from the previous single completion flag, so accurate recurring manual history begins when this migration is deployed.
