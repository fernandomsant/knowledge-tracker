ALTER TABLE dbo.StudyNotes
ADD NoteVersion BIGINT NOT NULL
    CONSTRAINT DF_StudyNotes_NoteVersion DEFAULT (1);

EXEC(N'
ALTER TABLE dbo.StudyNotes
ADD CONSTRAINT CK_StudyNotes_NoteVersion_Positive CHECK (NoteVersion > 0);
');

CREATE TABLE dbo.ClassificationTaxonomyState
(
    Id TINYINT NOT NULL,
    TaxonomyVersion BIGINT NOT NULL,
    UpdatedAtUtc DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_ClassificationTaxonomyState_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ClassificationTaxonomyState PRIMARY KEY (Id),
    CONSTRAINT CK_ClassificationTaxonomyState_Singleton CHECK (Id = 1),
    CONSTRAINT CK_ClassificationTaxonomyState_Version_Positive CHECK (TaxonomyVersion > 0)
);

INSERT INTO dbo.ClassificationTaxonomyState (Id, TaxonomyVersion)
VALUES (1, 1);

CREATE TABLE dbo.ClassificationJobs
(
    Id UNIQUEIDENTIFIER NOT NULL,
    NoteId UNIQUEIDENTIFIER NOT NULL,
    NoteVersion BIGINT NOT NULL,
    TaxonomyVersion BIGINT NOT NULL,
    Status TINYINT NOT NULL,
    Attempts INT NOT NULL CONSTRAINT DF_ClassificationJobs_Attempts DEFAULT (0),
    AvailableAtUtc DATETIMEOFFSET(7) NOT NULL,
    LockedUntilUtc DATETIMEOFFSET(7) NULL,
    WorkerId NVARCHAR(200) NULL,
    LastError NVARCHAR(2000) NULL,
    CreatedAtUtc DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_ClassificationJobs_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    StartedAtUtc DATETIMEOFFSET(7) NULL,
    CompletedAtUtc DATETIMEOFFSET(7) NULL,
    CONSTRAINT PK_ClassificationJobs PRIMARY KEY (Id),
    CONSTRAINT FK_ClassificationJobs_StudyNotes_NoteId
        FOREIGN KEY (NoteId) REFERENCES dbo.StudyNotes (Id) ON DELETE CASCADE,
    CONSTRAINT UX_ClassificationJobs_NoteVersionTaxonomy UNIQUE (NoteId, NoteVersion, TaxonomyVersion),
    CONSTRAINT CK_ClassificationJobs_NoteVersion_Positive CHECK (NoteVersion > 0),
    CONSTRAINT CK_ClassificationJobs_TaxonomyVersion_Positive CHECK (TaxonomyVersion > 0),
    CONSTRAINT CK_ClassificationJobs_Status CHECK (Status BETWEEN 0 AND 5),
    CONSTRAINT CK_ClassificationJobs_Attempts_NonNegative CHECK (Attempts >= 0)
);

CREATE INDEX IX_ClassificationJobs_Claim
    ON dbo.ClassificationJobs (Status, AvailableAtUtc, LockedUntilUtc, CreatedAtUtc)
    INCLUDE (NoteId, NoteVersion, TaxonomyVersion, Attempts);

CREATE TABLE dbo.ClassificationRuns
(
    Id UNIQUEIDENTIFIER NOT NULL,
    ClassificationJobId UNIQUEIDENTIFIER NOT NULL,
    NoteId UNIQUEIDENTIFIER NOT NULL,
    NoteVersion BIGINT NOT NULL,
    TaxonomyVersion BIGINT NOT NULL,
    Model NVARCHAR(200) NOT NULL,
    ModelVersion NVARCHAR(100) NOT NULL,
    CreatedAtUtc DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_ClassificationRuns_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ClassificationRuns PRIMARY KEY (Id),
    CONSTRAINT FK_ClassificationRuns_ClassificationJobs_JobId
        FOREIGN KEY (ClassificationJobId) REFERENCES dbo.ClassificationJobs (Id) ON DELETE CASCADE,
    CONSTRAINT UX_ClassificationRuns_ClassificationJobId UNIQUE (ClassificationJobId),
    CONSTRAINT CK_ClassificationRuns_NoteVersion_Positive CHECK (NoteVersion > 0),
    CONSTRAINT CK_ClassificationRuns_TaxonomyVersion_Positive CHECK (TaxonomyVersion > 0),
    CONSTRAINT CK_ClassificationRuns_Model_NotBlank CHECK (LEN(LTRIM(RTRIM(Model))) > 0),
    CONSTRAINT CK_ClassificationRuns_ModelVersion_NotBlank CHECK (LEN(LTRIM(RTRIM(ModelVersion))) > 0)
);

CREATE INDEX IX_ClassificationRuns_NoteVersion
    ON dbo.ClassificationRuns (NoteId, NoteVersion DESC, CreatedAtUtc DESC);

CREATE TABLE dbo.NoteClassifications
(
    ClassificationRunId UNIQUEIDENTIFIER NOT NULL,
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    SubjectName NVARCHAR(256) NOT NULL,
    Score DECIMAL(9, 8) NOT NULL,
    CONSTRAINT PK_NoteClassifications PRIMARY KEY (ClassificationRunId, SubjectId),
    CONSTRAINT FK_NoteClassifications_ClassificationRuns_RunId
        FOREIGN KEY (ClassificationRunId) REFERENCES dbo.ClassificationRuns (Id) ON DELETE CASCADE,
    CONSTRAINT CK_NoteClassifications_SubjectName_NotBlank CHECK (LEN(LTRIM(RTRIM(SubjectName))) > 0),
    CONSTRAINT CK_NoteClassifications_Score_Normalized CHECK (Score >= 0 AND Score <= 1)
);

CREATE TABLE dbo.StudyNoteSubjectRelations
(
    NoteId UNIQUEIDENTIFIER NOT NULL,
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    RelationSource TINYINT NOT NULL,
    Score DECIMAL(9, 8) NULL,
    ClassificationRunId UNIQUEIDENTIFIER NULL,
    CreatedAtUtc DATETIMEOFFSET(7) NOT NULL
        CONSTRAINT DF_StudyNoteSubjectRelations_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_StudyNoteSubjectRelations PRIMARY KEY (NoteId, SubjectId, RelationSource),
    CONSTRAINT FK_StudyNoteSubjectRelations_StudyNotes_NoteId
        FOREIGN KEY (NoteId) REFERENCES dbo.StudyNotes (Id) ON DELETE CASCADE,
    CONSTRAINT CK_StudyNoteSubjectRelations_Source CHECK (RelationSource IN (0, 1, 2)),
    CONSTRAINT CK_StudyNoteSubjectRelations_Score_Normalized CHECK (Score IS NULL OR (Score >= 0 AND Score <= 1)),
    CONSTRAINT CK_StudyNoteSubjectRelations_SourcePayload CHECK
    (
        (RelationSource = 0 AND Score IS NULL AND ClassificationRunId IS NULL)
        OR (RelationSource = 1 AND Score IS NOT NULL AND ClassificationRunId IS NOT NULL)
        OR RelationSource = 2
    )
);

CREATE INDEX IX_StudyNoteSubjectRelations_SubjectSource
    ON dbo.StudyNoteSubjectRelations (SubjectId, RelationSource, NoteId);

INSERT INTO dbo.StudyNoteSubjectRelations (NoteId, SubjectId, RelationSource, Score, ClassificationRunId)
SELECT note.Id, note.SubjectId, 0, NULL, NULL
FROM dbo.StudyNotes AS note;

EXEC(N'
INSERT INTO dbo.ClassificationJobs
    (Id, NoteId, NoteVersion, TaxonomyVersion, Status, Attempts, AvailableAtUtc)
SELECT NEWID(), note.Id, note.NoteVersion, taxonomy.TaxonomyVersion, 0, 0, SYSUTCDATETIME()
FROM dbo.StudyNotes AS note
CROSS JOIN dbo.ClassificationTaxonomyState AS taxonomy
WHERE taxonomy.Id = 1;
');

EXEC(N'
CREATE OR ALTER TRIGGER dbo.TR_Subjects_ClassificationTaxonomyVersion
ON dbo.Subjects
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ClassificationTaxonomyState
    SET TaxonomyVersion = TaxonomyVersion + 1,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE Id = 1;

    DELETE FROM dbo.StudyNoteSubjectRelations
    WHERE RelationSource = 1;

    INSERT INTO dbo.ClassificationJobs
        (Id, NoteId, NoteVersion, TaxonomyVersion, Status, Attempts, AvailableAtUtc)
    SELECT NEWID(), note.Id, note.NoteVersion, taxonomy.TaxonomyVersion, 0, 0, SYSUTCDATETIME()
    FROM dbo.StudyNotes AS note
    CROSS JOIN dbo.ClassificationTaxonomyState AS taxonomy
    WHERE taxonomy.Id = 1
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.ClassificationJobs AS existing
          WHERE existing.NoteId = note.Id
            AND existing.NoteVersion = note.NoteVersion
            AND existing.TaxonomyVersion = taxonomy.TaxonomyVersion
      );
END;
');
