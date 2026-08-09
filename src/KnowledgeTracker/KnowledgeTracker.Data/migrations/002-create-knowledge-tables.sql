CREATE TABLE dbo.Subjects
(
    Id UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(256) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    CONSTRAINT PK_Subjects PRIMARY KEY (Id),
    CONSTRAINT CK_Subjects_NameNotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);

CREATE TABLE dbo.StudyNotes
(
    Id UNIQUEIDENTIFIER NOT NULL,
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(512) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    StudyDurationTicks BIGINT NOT NULL,
    StudyStartedAtUtc DATETIMEOFFSET(7) NOT NULL,
    CONSTRAINT PK_StudyNotes PRIMARY KEY (Id),
    CONSTRAINT FK_StudyNotes_Subjects_SubjectId
        FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects (Id) ON DELETE CASCADE,
    CONSTRAINT CK_StudyNotes_TitleNotBlank CHECK (LEN(LTRIM(RTRIM(Title))) > 0),
    CONSTRAINT CK_StudyNotes_ContentNotBlank CHECK (LEN(LTRIM(RTRIM(Content))) > 0),
    CONSTRAINT CK_StudyNotes_StudyDurationNonNegative CHECK (StudyDurationTicks >= 0)
);

CREATE INDEX IX_StudyNotes_SubjectId_StudyStartedAtUtc
    ON dbo.StudyNotes (SubjectId, StudyStartedAtUtc DESC);

CREATE TABLE dbo.SubjectConnections
(
    Id UNIQUEIDENTIFIER NOT NULL,
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    ConnectedSubjectId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_SubjectConnections PRIMARY KEY (Id),
    CONSTRAINT FK_SubjectConnections_Subjects_SubjectId
        FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects (Id),
    CONSTRAINT FK_SubjectConnections_Subjects_ConnectedSubjectId
        FOREIGN KEY (ConnectedSubjectId) REFERENCES dbo.Subjects (Id),
    CONSTRAINT CK_SubjectConnections_DistinctSubjects
        CHECK (SubjectId <> ConnectedSubjectId),
    CONSTRAINT UX_SubjectConnections_SubjectPair UNIQUE (SubjectId, ConnectedSubjectId)
);

CREATE INDEX IX_SubjectConnections_ConnectedSubjectId
    ON dbo.SubjectConnections (ConnectedSubjectId);
