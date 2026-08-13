CREATE TABLE dbo.SubjectSubGoals
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SubjectSubGoals PRIMARY KEY,
    SubjectGoalId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(256) NOT NULL,
    IsCompleted BIT NOT NULL CONSTRAINT DF_SubjectSubGoals_IsCompleted DEFAULT 0,
    CompletedAtUtc DATETIMEOFFSET NULL,
    CreatedAtUtc DATETIMEOFFSET NOT NULL,
    CONSTRAINT FK_SubjectSubGoals_SubjectGoals FOREIGN KEY (SubjectGoalId) REFERENCES dbo.SubjectGoals (Id) ON DELETE CASCADE,
    CONSTRAINT CK_SubjectSubGoals_TitleNotBlank CHECK (LEN(LTRIM(RTRIM(Title))) > 0),
    CONSTRAINT CK_SubjectSubGoals_Completion CHECK ((IsCompleted = 0 AND CompletedAtUtc IS NULL) OR (IsCompleted = 1 AND CompletedAtUtc IS NOT NULL))
);
CREATE INDEX IX_SubjectSubGoals_SubjectGoalId ON dbo.SubjectSubGoals (SubjectGoalId, CreatedAtUtc);
