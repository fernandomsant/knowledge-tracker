CREATE TABLE dbo.SubjectGoalCompletions
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SubjectGoalCompletions PRIMARY KEY,
    SubjectGoalId UNIQUEIDENTIFIER NOT NULL,
    OccurrenceStartDate DATE NOT NULL,
    OccurrenceEndDate DATE NOT NULL,
    CompletedAtUtc DATETIMEOFFSET(7) NOT NULL,
    CompletionSource TINYINT NOT NULL CONSTRAINT DF_SubjectGoalCompletions_Source DEFAULT 4,
    CONSTRAINT FK_SubjectGoalCompletions_SubjectGoals
        FOREIGN KEY (SubjectGoalId) REFERENCES dbo.SubjectGoals (Id) ON DELETE CASCADE,
    CONSTRAINT UX_SubjectGoalCompletions_Occurrence
        UNIQUE (SubjectGoalId, OccurrenceStartDate, OccurrenceEndDate),
    CONSTRAINT CK_SubjectGoalCompletions_DateRange
        CHECK (OccurrenceStartDate <= OccurrenceEndDate),
    CONSTRAINT CK_SubjectGoalCompletions_Source
        CHECK (CompletionSource IN (1, 2, 3, 4))
);

CREATE INDEX IX_SubjectGoalCompletions_Occurrence
    ON dbo.SubjectGoalCompletions (OccurrenceStartDate, OccurrenceEndDate, SubjectGoalId)
    INCLUDE (CompletedAtUtc, CompletionSource);

-- Preserve the old permanent-completion timestamp for one-time goals only.
INSERT INTO dbo.SubjectGoalCompletions
    (Id, SubjectGoalId, OccurrenceStartDate, OccurrenceEndDate, CompletedAtUtc, CompletionSource)
SELECT
    NEWID(),
    goal.Id,
    CASE WHEN goal.GoalPeriod = 4 THEN goal.CustomPeriodStartDate ELSE CONVERT(date, goal.CreatedAtUtc) END,
    CASE
        WHEN goal.GoalPeriod = 4 THEN goal.CustomPeriodEndDate
        WHEN goal.TargetDate IS NOT NULL THEN goal.TargetDate
        WHEN goal.DeactivatedAtUtc IS NOT NULL THEN CONVERT(date, goal.DeactivatedAtUtc)
        ELSE CONVERT(date, SYSUTCDATETIME())
    END,
    goal.CompletedAtUtc,
    4
FROM dbo.SubjectGoals AS goal
WHERE goal.IsCompleted = 1
  AND goal.CompletedAtUtc IS NOT NULL
  AND goal.GoalPeriod IN (0, 4);

-- Reconstruct deterministic metric history from persisted notes. Older recurring
-- manual-completion history is intentionally not fabricated.
;WITH MetricGoals AS
(
    SELECT
        goal.Id AS SubjectGoalId,
        goal.SubjectId,
        goal.TopicId,
        goal.MetricDefinitionId,
        goal.TargetValue,
        goal.GoalPeriod,
        CONVERT(date, goal.CreatedAtUtc) AS CreatedDate,
        COALESCE(CONVERT(date, goal.DeactivatedAtUtc), CONVERT(date, SYSUTCDATETIME())) AS ActiveEndDate,
        goal.TargetDate,
        goal.CustomPeriodStartDate,
        goal.CustomPeriodEndDate
    FROM dbo.SubjectGoals AS goal
    WHERE goal.GoalKind = 1
), RecurringOccurrences AS
(
    SELECT SubjectGoalId, SubjectId, TopicId, MetricDefinitionId, TargetValue, GoalPeriod, CreatedDate, ActiveEndDate,
           CreatedDate AS OccurrenceStartDate,
           CreatedDate AS OccurrenceEndDate
    FROM MetricGoals
    WHERE GoalPeriod = 1
    UNION ALL
    SELECT SubjectGoalId, SubjectId, TopicId, MetricDefinitionId, TargetValue, GoalPeriod, CreatedDate, ActiveEndDate,
           DATEADD(day, -(DATEDIFF(day, CONVERT(date, '19000101'), CreatedDate) % 7), CreatedDate),
           DATEADD(day, 6, DATEADD(day, -(DATEDIFF(day, CONVERT(date, '19000101'), CreatedDate) % 7), CreatedDate))
    FROM MetricGoals
    WHERE GoalPeriod = 2
    UNION ALL
    SELECT SubjectGoalId, SubjectId, TopicId, MetricDefinitionId, TargetValue, GoalPeriod, CreatedDate, ActiveEndDate,
           DATEFROMPARTS(YEAR(CreatedDate), MONTH(CreatedDate), 1),
           EOMONTH(CreatedDate)
    FROM MetricGoals
    WHERE GoalPeriod = 3
    UNION ALL
    SELECT SubjectGoalId, SubjectId, TopicId, MetricDefinitionId, TargetValue, GoalPeriod, CreatedDate, ActiveEndDate,
           CustomPeriodStartDate, CustomPeriodEndDate
    FROM MetricGoals
    WHERE GoalPeriod = 4
    UNION ALL
    SELECT SubjectGoalId, SubjectId, TopicId, MetricDefinitionId, TargetValue, GoalPeriod, CreatedDate, ActiveEndDate,
           CreatedDate,
           CASE WHEN TargetDate IS NOT NULL AND TargetDate < ActiveEndDate THEN TargetDate ELSE ActiveEndDate END
    FROM MetricGoals
    WHERE GoalPeriod = 0
    UNION ALL
    SELECT occurrence.SubjectGoalId, occurrence.SubjectId, occurrence.TopicId, occurrence.MetricDefinitionId, occurrence.TargetValue, occurrence.GoalPeriod, occurrence.CreatedDate, occurrence.ActiveEndDate,
           CASE occurrence.GoalPeriod WHEN 1 THEN DATEADD(day, 1, occurrence.OccurrenceStartDate) WHEN 2 THEN DATEADD(day, 7, occurrence.OccurrenceStartDate) ELSE DATEADD(month, 1, occurrence.OccurrenceStartDate) END,
           CASE occurrence.GoalPeriod WHEN 1 THEN DATEADD(day, 1, occurrence.OccurrenceStartDate) WHEN 2 THEN DATEADD(day, 13, occurrence.OccurrenceStartDate) ELSE EOMONTH(DATEADD(month, 1, occurrence.OccurrenceStartDate)) END
    FROM RecurringOccurrences AS occurrence
    WHERE occurrence.GoalPeriod IN (1, 2, 3)
      AND occurrence.OccurrenceStartDate < occurrence.ActiveEndDate
), MetricTotals AS
(
    SELECT occurrence.SubjectGoalId, occurrence.OccurrenceStartDate, occurrence.OccurrenceEndDate, occurrence.TargetValue,
           SUM(CASE WHEN definition.NormalizedName = 'STUDY TIME' THEN note.StudyDurationTicks / 36000000000.0 ELSE metric.MetricValue END) AS TotalValue
    FROM RecurringOccurrences AS occurrence
    LEFT JOIN dbo.StudyNotes AS note
        ON note.SubjectId = occurrence.SubjectId
       AND note.TopicId = occurrence.TopicId
       AND CONVERT(date, note.StudyStartedAtUtc) BETWEEN occurrence.OccurrenceStartDate AND occurrence.OccurrenceEndDate
    LEFT JOIN dbo.StudyNoteMetrics AS metric
        ON metric.StudyNoteId = note.Id
       AND metric.MetricDefinitionId = occurrence.MetricDefinitionId
    LEFT JOIN dbo.StudyMetricDefinitions AS definition
        ON definition.Id = occurrence.MetricDefinitionId
    WHERE occurrence.OccurrenceStartDate <= occurrence.ActiveEndDate
      AND occurrence.OccurrenceEndDate >= occurrence.CreatedDate
    GROUP BY occurrence.SubjectGoalId, occurrence.OccurrenceStartDate, occurrence.OccurrenceEndDate, occurrence.TargetValue
)
INSERT INTO dbo.SubjectGoalCompletions
    (Id, SubjectGoalId, OccurrenceStartDate, OccurrenceEndDate, CompletedAtUtc, CompletionSource)
SELECT NEWID(), SubjectGoalId, OccurrenceStartDate, OccurrenceEndDate, SYSUTCDATETIME(), 4
FROM MetricTotals
WHERE TotalValue >= TargetValue
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.SubjectGoalCompletions AS existing
      WHERE existing.SubjectGoalId = MetricTotals.SubjectGoalId
        AND existing.OccurrenceStartDate = MetricTotals.OccurrenceStartDate
        AND existing.OccurrenceEndDate = MetricTotals.OccurrenceEndDate
  )
OPTION (MAXRECURSION 0);
