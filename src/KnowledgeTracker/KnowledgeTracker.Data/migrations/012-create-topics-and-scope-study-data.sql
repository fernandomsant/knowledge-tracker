CREATE TABLE dbo.Topics
(
    Id UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(256) NOT NULL,
    CONSTRAINT PK_Topics PRIMARY KEY (Id),
    CONSTRAINT CK_Topics_NameNotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);

INSERT INTO dbo.Topics (Id, Name)
SELECT Id, Name FROM dbo.Subjects;

EXEC(N'ALTER TABLE dbo.StudyNotes ADD TopicId UNIQUEIDENTIFIER NULL;');
EXEC(N'ALTER TABLE dbo.SubjectGoals ADD TopicId UNIQUEIDENTIFIER NULL;');
EXEC(N'UPDATE dbo.StudyNotes SET TopicId = SubjectId;');
EXEC(N'UPDATE dbo.SubjectGoals SET TopicId = SubjectId;');
EXEC(N'ALTER TABLE dbo.StudyNotes ALTER COLUMN TopicId UNIQUEIDENTIFIER NOT NULL;');
EXEC(N'ALTER TABLE dbo.SubjectGoals ALTER COLUMN TopicId UNIQUEIDENTIFIER NOT NULL;');
EXEC(N'ALTER TABLE dbo.StudyNotes ADD CONSTRAINT FK_StudyNotes_Topics_TopicId FOREIGN KEY (TopicId) REFERENCES dbo.Topics (Id);');
EXEC(N'ALTER TABLE dbo.SubjectGoals ADD CONSTRAINT FK_SubjectGoals_Topics_TopicId FOREIGN KEY (TopicId) REFERENCES dbo.Topics (Id);');
EXEC(N'CREATE INDEX IX_StudyNotes_TopicId_StudyStartedAtUtc ON dbo.StudyNotes (TopicId, StudyStartedAtUtc DESC);');
EXEC(N'CREATE INDEX IX_SubjectGoals_TopicId ON dbo.SubjectGoals (TopicId);');
