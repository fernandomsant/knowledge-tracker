CREATE TABLE dbo.SubjectGoalDayRecords
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SubjectGoalDayRecords PRIMARY KEY,
    SubjectGoalId UNIQUEIDENTIFIER NOT NULL,
    OccurredOn DATE NOT NULL,
    IsCompleted BIT NOT NULL,
    RecordedAtUtc DATETIMEOFFSET NOT NULL,
    CONSTRAINT FK_SubjectGoalDayRecords_SubjectGoals FOREIGN KEY (SubjectGoalId) REFERENCES dbo.SubjectGoals (Id) ON DELETE CASCADE,
    CONSTRAINT UQ_SubjectGoalDayRecords_GoalDate UNIQUE (SubjectGoalId, OccurredOn)
);

CREATE INDEX IX_SubjectGoalDayRecords_SubjectGoalId_OccurredOn ON dbo.SubjectGoalDayRecords (SubjectGoalId, OccurredOn DESC);

INSERT INTO dbo.SubjectGoalDayRecords (Id, SubjectGoalId, OccurredOn, IsCompleted, RecordedAtUtc)
SELECT NEWID(), Id, CONVERT(DATE, CompletedAtUtc), 1, CompletedAtUtc
FROM dbo.SubjectGoals
WHERE IsCompleted = 1;
