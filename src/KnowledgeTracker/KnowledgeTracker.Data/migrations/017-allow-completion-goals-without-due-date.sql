ALTER TABLE dbo.SubjectGoals DROP CONSTRAINT CK_SubjectGoals_Target;

ALTER TABLE dbo.SubjectGoals
ADD CONSTRAINT CK_SubjectGoals_Target CHECK
(
    (GoalKind = 1 AND MetricDefinitionId IS NOT NULL AND TargetValue > 0 AND TargetDate IS NULL)
    OR (GoalKind = 2 AND MetricDefinitionId IS NULL AND TargetValue IS NULL)
);
