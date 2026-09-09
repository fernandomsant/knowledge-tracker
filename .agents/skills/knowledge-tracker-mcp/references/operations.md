# Knowledge Tracker entity model and operations

## Ownership and relationships

```text
Workspace
+-- Subject
    +-- parentSubjectId -> Subject (optional; subjects form a tree)
    +-- Topic
    +-- StudyNote
    |   +-- topicId -> Topic owned by the same subject
    |   +-- metrics[] -> StudyMetricDefinition
    +-- SubjectGoal
        +-- topicId -> Topic owned by the same subject
        +-- MetricTarget: metricDefinitionId -> StudyMetricDefinition
        +-- TargetDate: subGoals[] -> SubjectSubGoal
```

| Entity | Relationship and constraint |
| --- | --- |
| Workspace | The MCP boundary is workspace-scoped by `workspaceName`. Every subject, topic, note, and goal is resolved inside that workspace. |
| Subject | Has an optional `parentSubjectId`; this creates a tree. A subject cannot be its own parent. |
| Topic | Belongs to exactly one subject. Its ID is valid only for notes and goals on that same subject. |
| Study note | Belongs to exactly one **leaf** subject and exactly one of that subject's topics. A note may contain metric values, each tied to a metric definition. |
| Goal | Belongs to one subject and one of that subject's topics. Goals can belong to parent subjects as well as leaves. |
| MetricTarget goal | Requires a metric definition and a positive target. It is completed automatically when qualifying notes reach that target; it cannot be manually completed. |
| TargetDate goal | May have a due date and optional checklist sub-goals. It can be manually completed. |
| SubjectSubGoal | Belongs to one TargetDate goal. MetricTarget goals cannot have sub-goals. |
| Metric definition | A reusable definition referenced by note metrics and MetricTarget goals. This MCP tool set does not list definitions; use only a known ID. |

## How note and goal progress relate

- A note contributes to metric progress using its study date and metric values.
- A leaf subject's goal considers notes tagged with the goal's topic.
- A parent subject's goal includes qualifying notes from its descendant subjects.
- A note create or update re-evaluates metric goals for its subject.

## Operation map

All operations require `workspaceName`; IDs come from prior MCP responses.

| Operation | Scope | Entity | Inputs beyond workspace |
| --- | --- | --- | --- |
| `list_subjects` | `subjects:read` | Subject tree | - |
| `get_subject` | `subjects:read` | Subject and direct details | `subjectId` |
| `create_subject` | `subjects:write` | Subject | `name`; optional `description`, `parentSubjectId` |
| `list_topics` | `topics:read` | Topics | - |
| `create_topic` | `topics:write` | Topic | `subjectId`, `name` |
| `list_notes` | `notes:read` | Notes | `subjectId`, `includeDescendants` |
| `create_note` | `notes:write` | Study note | `subjectId`, `topicId`, `title`, `content`, `studyDurationMinutes`, `studyStartedAtUtc`; optional `metrics: [{ definitionId, value }]` |
| `list_goals` | `goals:read` | Goals | `subjectId` |
| `create_goal` | `goals:write` | Goal | `subjectId`, `topicId`, `title`, `kind`; see goal rules |
| `complete_goal` | `goals:write` | TargetDate goal | `goalId` |
| `list_goal_activity` | `goals:read` | Goal activity | inclusive `from`, `to` |

## Goal request rules

- `kind` is `MetricTarget` or `TargetDate`.
- `MetricTarget` requires `metricDefinitionId` and positive `targetValue`; omit `targetDate` and `subGoals`.
- `TargetDate` may include `targetDate` and `subGoals`.
- `period` defaults to `AllTime`; allowed values are `AllTime`, `Daily`, `Weekly`, `Monthly`, and `Custom`.
- `Custom` requires both `periodStartDate` and `periodEndDate` as ISO-8601 dates.
