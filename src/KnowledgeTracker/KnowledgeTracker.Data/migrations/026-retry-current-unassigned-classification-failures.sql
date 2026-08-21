UPDATE job
SET job.Status = 0,
    job.Attempts = 0,
    job.AvailableAtUtc = SYSUTCDATETIME(),
    job.LockedUntilUtc = NULL,
    job.WorkerId = NULL,
    job.CompletedAtUtc = NULL,
    job.LastError = NULL
FROM dbo.ClassificationJobs AS job
INNER JOIN dbo.StudyNotes AS note ON note.Id = job.NoteId
CROSS JOIN dbo.ClassificationTaxonomyState AS taxonomy
WHERE taxonomy.Id = 1
  AND job.Status = 4
  AND job.NoteVersion = note.NoteVersion
  AND job.TaxonomyVersion = taxonomy.TaxonomyVersion
  AND note.SubjectId IS NULL
  AND note.TopicId IS NULL;
