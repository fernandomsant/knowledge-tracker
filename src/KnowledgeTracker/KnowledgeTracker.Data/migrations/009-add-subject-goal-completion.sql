ALTER TABLE dbo.SubjectGoals
ADD IsCompleted BIT NOT NULL CONSTRAINT DF_SubjectGoals_IsCompleted DEFAULT 0,
    CompletedAtUtc DATETIMEOFFSET NULL;

EXEC(N'
ALTER TABLE dbo.SubjectGoals
ADD CONSTRAINT CK_SubjectGoals_Completion CHECK
(
    (GoalKind = 1 AND IsCompleted = 0 AND CompletedAtUtc IS NULL)
    OR (GoalKind = 2 AND ((IsCompleted = 0 AND CompletedAtUtc IS NULL) OR (IsCompleted = 1 AND CompletedAtUtc IS NOT NULL)))
);');
