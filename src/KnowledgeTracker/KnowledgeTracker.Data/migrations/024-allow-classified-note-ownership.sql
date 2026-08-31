ALTER TABLE dbo.StudyNotes DROP CONSTRAINT FK_StudyNotes_Topics_SubjectScope;
ALTER TABLE dbo.StudyNotes DROP CONSTRAINT FK_StudyNotes_Subjects_SubjectId;
DROP INDEX IX_StudyNotes_SubjectId_StudyStartedAtUtc ON dbo.StudyNotes;

ALTER TABLE dbo.StudyNotes ALTER COLUMN SubjectId UNIQUEIDENTIFIER NULL;
ALTER TABLE dbo.StudyNotes ALTER COLUMN TopicId UNIQUEIDENTIFIER NULL;

ALTER TABLE dbo.StudyNotes ADD CONSTRAINT CK_StudyNotes_OwnershipComplete
    CHECK
    (
        (SubjectId IS NULL AND TopicId IS NULL)
        OR (SubjectId IS NOT NULL AND TopicId IS NOT NULL)
    );
ALTER TABLE dbo.StudyNotes ADD CONSTRAINT FK_StudyNotes_Subjects_SubjectId
    FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects (Id) ON DELETE CASCADE;
ALTER TABLE dbo.StudyNotes ADD CONSTRAINT FK_StudyNotes_Topics_SubjectScope
    FOREIGN KEY (TopicId, SubjectId) REFERENCES dbo.Topics (Id, SubjectId);

CREATE INDEX IX_StudyNotes_SubjectId_StudyStartedAtUtc
    ON dbo.StudyNotes (SubjectId, StudyStartedAtUtc DESC);

DROP TABLE dbo.NoteClassifications;

CREATE TABLE dbo.NoteClassifications
(
    ClassificationRunId UNIQUEIDENTIFIER NOT NULL,
    TopicId UNIQUEIDENTIFIER NOT NULL,
    TopicName NVARCHAR(256) NOT NULL,
    Score DECIMAL(9, 8) NOT NULL,
    CONSTRAINT PK_NoteClassifications PRIMARY KEY (ClassificationRunId, TopicId),
    CONSTRAINT FK_NoteClassifications_ClassificationRuns_RunId
        FOREIGN KEY (ClassificationRunId) REFERENCES dbo.ClassificationRuns (Id) ON DELETE CASCADE,
    CONSTRAINT CK_NoteClassifications_TopicName_NotBlank CHECK (LEN(LTRIM(RTRIM(TopicName))) > 0),
    CONSTRAINT CK_NoteClassifications_Score_Normalized CHECK (Score >= 0 AND Score <= 1)
);

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

EXEC(N'
CREATE OR ALTER TRIGGER dbo.TR_Topics_ClassificationTaxonomyVersion
ON dbo.Topics
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
