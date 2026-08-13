CREATE TABLE dbo.SubjectGoals
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SubjectGoals PRIMARY KEY,
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(256) NOT NULL,
    GoalKind TINYINT NOT NULL,
    MetricDefinitionId UNIQUEIDENTIFIER NULL,
    TargetValue DECIMAL(18, 2) NULL,
    TargetDate DATE NULL,
    CreatedAtUtc DATETIMEOFFSET NOT NULL,
    CONSTRAINT FK_SubjectGoals_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects (Id) ON DELETE CASCADE,
    CONSTRAINT FK_SubjectGoals_StudyMetricDefinitions FOREIGN KEY (MetricDefinitionId) REFERENCES dbo.StudyMetricDefinitions (Id),
    CONSTRAINT CK_SubjectGoals_TitleNotBlank CHECK (LEN(LTRIM(RTRIM(Title))) > 0),
    CONSTRAINT CK_SubjectGoals_Kind CHECK (GoalKind IN (1, 2)),
    CONSTRAINT CK_SubjectGoals_Target CHECK
    (
        (GoalKind = 1 AND MetricDefinitionId IS NOT NULL AND TargetValue > 0 AND TargetDate IS NULL)
        OR (GoalKind = 2 AND MetricDefinitionId IS NULL AND TargetValue IS NULL AND TargetDate IS NOT NULL)
    )
);
CREATE INDEX IX_SubjectGoals_SubjectId ON dbo.SubjectGoals (SubjectId, CreatedAtUtc DESC);
