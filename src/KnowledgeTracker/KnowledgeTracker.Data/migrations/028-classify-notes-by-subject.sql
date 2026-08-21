CREATE TABLE dbo.NoteSubjectClassifications
(
    ClassificationRunId UNIQUEIDENTIFIER NOT NULL,
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    SubjectName NVARCHAR(256) NOT NULL,
    Score DECIMAL(9, 8) NOT NULL
);

INSERT INTO dbo.NoteSubjectClassifications (ClassificationRunId, SubjectId, SubjectName, Score)
SELECT classification.ClassificationRunId,
       topic.SubjectId,
       subject.Name,
       MAX(classification.Score)
FROM dbo.NoteClassifications AS classification
INNER JOIN dbo.Topics AS topic ON topic.Id = classification.TopicId
INNER JOIN dbo.Subjects AS subject ON subject.Id = topic.SubjectId
GROUP BY classification.ClassificationRunId, topic.SubjectId, subject.Name;

DROP TABLE dbo.NoteClassifications;

ALTER TABLE dbo.NoteSubjectClassifications ADD
    CONSTRAINT PK_NoteClassifications PRIMARY KEY (ClassificationRunId, SubjectId),
    CONSTRAINT FK_NoteClassifications_ClassificationRuns_RunId
        FOREIGN KEY (ClassificationRunId) REFERENCES dbo.ClassificationRuns (Id) ON DELETE CASCADE,
    CONSTRAINT CK_NoteClassifications_SubjectName_NotBlank CHECK (LEN(LTRIM(RTRIM(SubjectName))) > 0),
    CONSTRAINT CK_NoteClassifications_Score_Normalized CHECK (Score >= 0 AND Score <= 1);

EXEC sys.sp_rename N'dbo.NoteSubjectClassifications', N'NoteClassifications';

CREATE INDEX IX_NoteClassifications_SubjectId
    ON dbo.NoteClassifications (SubjectId, ClassificationRunId);

CREATE TABLE #ClassifierOwnedNotes
(
    NoteId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY
);

INSERT INTO #ClassifierOwnedNotes (NoteId)
SELECT note.Id
FROM dbo.StudyNotes AS note
WHERE note.SubjectId IS NOT NULL
  AND note.TopicId IS NOT NULL
  AND EXISTS
  (
      SELECT 1
      FROM dbo.ClassificationRuns AS run
      WHERE run.NoteId = note.Id
        AND run.NoteVersion = note.NoteVersion
  );

UPDATE note
SET note.SubjectId = NULL,
    note.TopicId = NULL
FROM dbo.StudyNotes AS note
INNER JOIN #ClassifierOwnedNotes AS classified ON classified.NoteId = note.Id;

DELETE FROM dbo.StudyNoteSubjectRelations
WHERE RelationSource = 1;

UPDATE dbo.ClassificationTaxonomyState
SET TaxonomyVersion = TaxonomyVersion + 1,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE Id = 1;

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
