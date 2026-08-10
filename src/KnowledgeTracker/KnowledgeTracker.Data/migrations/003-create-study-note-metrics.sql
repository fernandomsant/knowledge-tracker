CREATE TABLE dbo.StudyNoteMetrics
(
    StudyNoteId UNIQUEIDENTIFIER NOT NULL,
    NormalizedName NVARCHAR(256) NOT NULL,
    Name NVARCHAR(256) NOT NULL,
    MetricValue DECIMAL(18, 2) NOT NULL,
    CONSTRAINT PK_StudyNoteMetrics PRIMARY KEY (StudyNoteId, NormalizedName),
    CONSTRAINT FK_StudyNoteMetrics_StudyNotes_StudyNoteId
        FOREIGN KEY (StudyNoteId) REFERENCES dbo.StudyNotes (Id) ON DELETE CASCADE,
    CONSTRAINT CK_StudyNoteMetrics_NameNotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
    CONSTRAINT CK_StudyNoteMetrics_ValueNonNegative CHECK (MetricValue >= 0)
);
