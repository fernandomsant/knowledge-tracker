ALTER TABLE dbo.SubjectGoals
ADD GoalPeriod TINYINT NOT NULL CONSTRAINT DF_SubjectGoals_GoalPeriod DEFAULT 0,
    CustomPeriodStartDate DATE NULL,
    CustomPeriodEndDate DATE NULL;

EXEC(N'
ALTER TABLE dbo.SubjectGoals
ADD CONSTRAINT CK_SubjectGoals_Period CHECK
(
    (GoalKind = 1 AND
        (
            (GoalPeriod = 0 AND CustomPeriodStartDate IS NULL AND CustomPeriodEndDate IS NULL)
            OR (GoalPeriod IN (1, 2, 3) AND CustomPeriodStartDate IS NULL AND CustomPeriodEndDate IS NULL)
            OR (GoalPeriod = 4 AND CustomPeriodStartDate IS NOT NULL AND CustomPeriodEndDate IS NOT NULL AND CustomPeriodStartDate <= CustomPeriodEndDate)
        ))
    OR (GoalKind = 2 AND GoalPeriod = 0 AND CustomPeriodStartDate IS NULL AND CustomPeriodEndDate IS NULL)
);');
