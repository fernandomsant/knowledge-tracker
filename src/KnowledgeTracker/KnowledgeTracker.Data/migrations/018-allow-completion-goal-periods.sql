ALTER TABLE dbo.SubjectGoals DROP CONSTRAINT CK_SubjectGoals_Period;

ALTER TABLE dbo.SubjectGoals
ADD CONSTRAINT CK_SubjectGoals_Period CHECK
(
    (GoalPeriod IN (0, 1, 2, 3) AND CustomPeriodStartDate IS NULL AND CustomPeriodEndDate IS NULL)
    OR (GoalPeriod = 4 AND CustomPeriodStartDate IS NOT NULL AND CustomPeriodEndDate IS NOT NULL AND CustomPeriodStartDate <= CustomPeriodEndDate)
);
