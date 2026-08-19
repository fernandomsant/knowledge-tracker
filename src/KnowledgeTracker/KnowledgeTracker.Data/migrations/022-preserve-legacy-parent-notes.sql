IF OBJECT_ID(N'dbo.TR_StudyNotes_LeafSubjectOnly', N'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_StudyNotes_LeafSubjectOnly;

UPDATE note
SET
    note.SubjectId = audit.SourceSubjectId,
    note.TopicId = audit.SourceTopicId
FROM dbo.StudyNotes AS note
INNER JOIN dbo.SubjectNoteOwnershipMigrations AS audit ON audit.StudyNoteId = note.Id
INNER JOIN dbo.Topics AS sourceTopic
    ON sourceTopic.Id = audit.SourceTopicId
   AND sourceTopic.SubjectId = audit.SourceSubjectId;

EXEC(N'
CREATE TRIGGER dbo.TR_StudyNotes_LeafSubjectOnly
ON dbo.StudyNotes
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS note
        INNER JOIN dbo.Subjects AS subject ON subject.Id = note.SubjectId
        WHERE EXISTS
        (
            SELECT 1
            FROM dbo.Subjects AS child
            WHERE child.ParentSubjectId = subject.Id
        )
    )
        THROW 51001, ''New study notes can only belong to leaf subjects.'', 1;
END;
');

EXEC(N'
CREATE TRIGGER dbo.TR_StudyNotes_PreventParentMove
ON dbo.StudyNotes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS currentNote
        INNER JOIN deleted AS previousNote ON previousNote.Id = currentNote.Id
        INNER JOIN dbo.Subjects AS subject ON subject.Id = currentNote.SubjectId
        WHERE currentNote.SubjectId <> previousNote.SubjectId
          AND EXISTS
          (
              SELECT 1
              FROM dbo.Subjects AS child
              WHERE child.ParentSubjectId = subject.Id
          )
    )
        THROW 51004, ''Study notes cannot be moved to parent subjects.'', 1;
END;
');

EXEC(N'
CREATE OR ALTER TRIGGER dbo.TR_Subjects_LeafNoteOwnership
ON dbo.Subjects
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted AS newChild
        WHERE newChild.ParentSubjectId IS NOT NULL
          AND NOT EXISTS
          (
              SELECT 1
              FROM deleted AS oldChild
              WHERE oldChild.Id = newChild.Id
                AND oldChild.ParentSubjectId = newChild.ParentSubjectId
          )
          AND EXISTS
          (
              SELECT 1
              FROM dbo.StudyNotes AS note
              WHERE note.SubjectId = newChild.ParentSubjectId
          )
          AND NOT EXISTS
          (
              SELECT 1
              FROM deleted AS existingChild
              WHERE existingChild.ParentSubjectId = newChild.ParentSubjectId
          )
    )
        THROW 51002, ''A new child cannot be added beneath a subject with direct study notes.'', 1;

    DECLARE @InvalidHierarchy BIT = 0;

    ;WITH Hierarchy AS
    (
        SELECT
            subject.Id AS RootId,
            subject.Id AS CurrentId,
            subject.ParentSubjectId,
            CAST(1 AS INT) AS Depth,
            CAST(CONCAT(''/'', CONVERT(VARCHAR(36), subject.Id), ''/'') AS VARCHAR(MAX)) AS HierarchyPath,
            CAST(0 AS BIT) AS CycleDetected
        FROM dbo.Subjects AS subject

        UNION ALL

        SELECT
            hierarchy.RootId,
            parent.Id,
            parent.ParentSubjectId,
            hierarchy.Depth + 1,
            CAST(CONCAT(hierarchy.HierarchyPath, CONVERT(VARCHAR(36), parent.Id), ''/'') AS VARCHAR(MAX)),
            CAST(CASE WHEN CHARINDEX(CONCAT(''/'', CONVERT(VARCHAR(36), parent.Id), ''/''), hierarchy.HierarchyPath) > 0 THEN 1 ELSE 0 END AS BIT)
        FROM Hierarchy AS hierarchy
        INNER JOIN dbo.Subjects AS parent ON parent.Id = hierarchy.ParentSubjectId
        WHERE hierarchy.ParentSubjectId IS NOT NULL
          AND hierarchy.CycleDetected = 0
    )
    SELECT TOP (1) @InvalidHierarchy = 1
    FROM Hierarchy
    WHERE CycleDetected = 1 OR Depth > 4
    OPTION (MAXRECURSION 100);

    IF @InvalidHierarchy = 1
        THROW 51003, ''Subject hierarchy contains a cycle or exceeds four levels.'', 1;
END;
');
