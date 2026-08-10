ALTER TABLE dbo.Subjects ADD ParentSubjectId UNIQUEIDENTIFIER NULL;
EXEC(N'
    ALTER TABLE dbo.Subjects
        ADD CONSTRAINT FK_Subjects_ParentSubjectId
        FOREIGN KEY (ParentSubjectId) REFERENCES dbo.Subjects (Id);
');
EXEC(N'
    ALTER TABLE dbo.Subjects
        ADD CONSTRAINT CK_Subjects_NotOwnParent
        CHECK (ParentSubjectId IS NULL OR ParentSubjectId <> Id);
');
EXEC(N'CREATE INDEX IX_Subjects_ParentSubjectId ON dbo.Subjects (ParentSubjectId);');
