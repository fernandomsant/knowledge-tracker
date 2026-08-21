UPDATE job
SET job.Status = 5,
    job.LockedUntilUtc = NULL,
    job.WorkerId = NULL,
    job.CompletedAtUtc = SYSUTCDATETIME(),
    job.LastError = N'Superseded because the note is already classified.'
FROM dbo.ClassificationJobs AS job
INNER JOIN dbo.StudyNotes AS note ON note.Id = job.NoteId
WHERE job.Status IN (0, 1, 2)
  AND note.SubjectId IS NOT NULL
  AND note.TopicId IS NOT NULL;

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

    DELETE relation
    FROM dbo.StudyNoteSubjectRelations AS relation
    INNER JOIN dbo.StudyNotes AS note ON note.Id = relation.NoteId
    WHERE relation.RelationSource = 1
      AND note.SubjectId IS NULL
      AND note.TopicId IS NULL;

    INSERT INTO dbo.ClassificationJobs
        (Id, NoteId, NoteVersion, TaxonomyVersion, Status, Attempts, AvailableAtUtc)
    SELECT NEWID(), note.Id, note.NoteVersion, taxonomy.TaxonomyVersion, 0, 0, SYSUTCDATETIME()
    FROM dbo.StudyNotes AS note
    CROSS JOIN dbo.ClassificationTaxonomyState AS taxonomy
    WHERE taxonomy.Id = 1
      AND note.SubjectId IS NULL
      AND note.TopicId IS NULL
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

    DELETE relation
    FROM dbo.StudyNoteSubjectRelations AS relation
    INNER JOIN dbo.StudyNotes AS note ON note.Id = relation.NoteId
    WHERE relation.RelationSource = 1
      AND note.SubjectId IS NULL
      AND note.TopicId IS NULL;

    INSERT INTO dbo.ClassificationJobs
        (Id, NoteId, NoteVersion, TaxonomyVersion, Status, Attempts, AvailableAtUtc)
    SELECT NEWID(), note.Id, note.NoteVersion, taxonomy.TaxonomyVersion, 0, 0, SYSUTCDATETIME()
    FROM dbo.StudyNotes AS note
    CROSS JOIN dbo.ClassificationTaxonomyState AS taxonomy
    WHERE taxonomy.Id = 1
      AND note.SubjectId IS NULL
      AND note.TopicId IS NULL
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
