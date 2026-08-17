ALTER TABLE dbo.SubjectGoals
ADD IsActive BIT NOT NULL CONSTRAINT DF_SubjectGoals_IsActive DEFAULT 1,
    DeactivatedAtUtc DATETIMEOFFSET NULL;

EXEC(N'
ALTER TABLE dbo.SubjectGoals
ADD CONSTRAINT CK_SubjectGoals_Activation CHECK
(
    (IsActive = 1 AND DeactivatedAtUtc IS NULL)
    OR (IsActive = 0 AND DeactivatedAtUtc IS NOT NULL)
);');

CREATE INDEX IX_SubjectGoals_ActiveSubjectPriority ON dbo.SubjectGoals (SubjectId, PriorityPosition) WHERE IsActive = 1;
