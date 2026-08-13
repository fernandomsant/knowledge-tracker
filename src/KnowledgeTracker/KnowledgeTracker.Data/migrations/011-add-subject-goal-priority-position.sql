ALTER TABLE dbo.SubjectGoals ADD PriorityPosition BIGINT NULL;

EXEC(N'
;WITH OrderedGoals AS
(
    SELECT Id, ROW_NUMBER() OVER (ORDER BY CreatedAtUtc, Id) AS PriorityPosition
    FROM dbo.SubjectGoals
)
UPDATE Goals SET PriorityPosition = OrderedGoals.PriorityPosition
FROM dbo.SubjectGoals Goals
JOIN OrderedGoals ON OrderedGoals.Id = Goals.Id;

ALTER TABLE dbo.SubjectGoals ALTER COLUMN PriorityPosition BIGINT NOT NULL;
CREATE UNIQUE INDEX UX_SubjectGoals_PriorityPosition ON dbo.SubjectGoals (PriorityPosition);
');
