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

-- Reconstruct the currently applicable metric occurrence from persisted notes.
-- Older recurring manual-completion history is intentionally not fabricated.
;WITH CurrentOccurrences AS
(
    SELECT
        goal.Id AS SubjectGoalId,
        goal.SubjectId,
        goal.TopicId,
        goal.MetricDefinitionId,
        goal.TargetValue,
        goal.GoalPeriod,
        CASE
            WHEN goal.GoalPeriod = 1 THEN CONVERT(date, SYSUTCDATETIME())
            WHEN goal.GoalPeriod = 2 THEN DATEADD(day, -(DATEDIFF(day, CONVERT(date, '19000101'), CONVERT(date, SYSUTCDATETIME())) % 7), CONVERT(date, SYSUTCDATETIME()))
            WHEN goal.GoalPeriod = 3 THEN DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1)
            WHEN goal.GoalPeriod = 4 THEN goal.CustomPeriodStartDate
            ELSE CONVERT(date, goal.CreatedAtUtc)
        END AS OccurrenceStartDate,
        CASE
            WHEN goal.GoalPeriod = 1 THEN CONVERT(date, SYSUTCDATETIME())
            WHEN goal.GoalPeriod = 2 THEN DATEADD(day, 6, DATEADD(day, -(DATEDIFF(day, CONVERT(date, '19000101'), CONVERT(date, SYSUTCDATETIME())) % 7), CONVERT(date, SYSUTCDATETIME())))
            WHEN goal.GoalPeriod = 3 THEN EOMONTH(SYSUTCDATETIME())
            WHEN goal.GoalPeriod = 4 THEN goal.CustomPeriodEndDate
            ELSE COALESCE(CONVERT(date, goal.DeactivatedAtUtc), CONVERT(date, SYSUTCDATETIME()))
        END AS OccurrenceEndDate
    FROM dbo.SubjectGoals AS goal
    WHERE goal.GoalKind = 1
      AND goal.IsActive = 1
), MetricTotals AS
(
    SELECT
        occurrence.SubjectGoalId,
        occurrence.OccurrenceStartDate,
        occurrence.OccurrenceEndDate,
        occurrence.TargetValue,
        SUM(CASE
            WHEN definition.NormalizedName = 'STUDY TIME' THEN note.StudyDurationTicks / 36000000000.0
            ELSE metric.MetricValue
        END) AS TotalValue
    FROM CurrentOccurrences AS occurrence
    LEFT JOIN dbo.StudyNotes AS note
        ON note.SubjectId = occurrence.SubjectId
       AND note.TopicId = occurrence.TopicId
       AND CONVERT(date, note.StudyStartedAtUtc) BETWEEN occurrence.OccurrenceStartDate AND occurrence.OccurrenceEndDate
    LEFT JOIN dbo.StudyNoteMetrics AS metric
        ON metric.StudyNoteId = note.Id
       AND metric.MetricDefinitionId = occurrence.MetricDefinitionId
    LEFT JOIN dbo.StudyMetricDefinitions AS definition
        ON definition.Id = occurrence.MetricDefinitionId
    GROUP BY occurrence.SubjectGoalId, occurrence.OccurrenceStartDate, occurrence.OccurrenceEndDate, occurrence.TargetValue
)
INSERT INTO dbo.SubjectGoalCompletions
    (Id, SubjectGoalId, OccurrenceStartDate, OccurrenceEndDate, CompletedAtUtc, CompletionSource)
SELECT NEWID(), SubjectGoalId, OccurrenceStartDate, OccurrenceEndDate, SYSUTCDATETIME(), 4
FROM MetricTotals
WHERE TotalValue >= TargetValue;
