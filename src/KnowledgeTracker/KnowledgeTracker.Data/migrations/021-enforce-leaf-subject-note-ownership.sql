CREATE TABLE dbo.SubjectNoteOwnershipMigrations
(
    Id BIGINT IDENTITY(1, 1) NOT NULL,
    MigrationKey UNIQUEIDENTIFIER NOT NULL,
    StudyNoteId UNIQUEIDENTIFIER NOT NULL,
    SourceSubjectId UNIQUEIDENTIFIER NOT NULL,
    SourceTopicId UNIQUEIDENTIFIER NOT NULL,
    TargetSubjectId UNIQUEIDENTIFIER NOT NULL,
    TargetTopicId UNIQUEIDENTIFIER NOT NULL,
    MigratedAtUtc DATETIME2(7) NOT NULL,
    CONSTRAINT PK_SubjectNoteOwnershipMigrations PRIMARY KEY (Id)
);

DECLARE @MigrationKey UNIQUEIDENTIFIER = NEWID();

CREATE TABLE #SubjectNoteConflicts
(
    StudyNoteId UNIQUEIDENTIFIER NOT NULL,
    SourceSubjectId UNIQUEIDENTIFIER NOT NULL,
    SourceTopicId UNIQUEIDENTIFIER NOT NULL,
    TargetSubjectId UNIQUEIDENTIFIER NOT NULL,
    TopicName NVARCHAR(256) NOT NULL,
    CONSTRAINT PK_SubjectNoteConflicts PRIMARY KEY (StudyNoteId)
);

;WITH DescendantTree AS
(
    SELECT
        subject.Id AS AncestorId,
        subject.Id AS DescendantId,
        CAST('/' + CONVERT(VARCHAR(36), subject.Id) + '/' AS VARCHAR(MAX)) AS HierarchyPath
    FROM dbo.Subjects AS subject

    UNION ALL

    SELECT
        tree.AncestorId,
        child.Id,
        CAST(tree.HierarchyPath + CONVERT(VARCHAR(36), child.Id) + '/' AS VARCHAR(MAX))
    FROM DescendantTree AS tree
    INNER JOIN dbo.Subjects AS child ON child.ParentSubjectId = tree.DescendantId
    WHERE CHARINDEX('/' + CONVERT(VARCHAR(36), child.Id) + '/', tree.HierarchyPath) = 0
),
LeafChoices AS
(
    SELECT
        tree.AncestorId,
        tree.DescendantId,
        ROW_NUMBER() OVER (PARTITION BY tree.AncestorId ORDER BY tree.HierarchyPath) AS ChoiceOrder
    FROM DescendantTree AS tree
    WHERE tree.DescendantId <> tree.AncestorId
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.Subjects AS child
          WHERE child.ParentSubjectId = tree.DescendantId
      )
)
INSERT INTO #SubjectNoteConflicts
(
    StudyNoteId,
    SourceSubjectId,
    SourceTopicId,
    TargetSubjectId,
    TopicName
)
SELECT
    note.Id,
    note.SubjectId,
    note.TopicId,
    leaf.DescendantId,
    topic.Name
FROM dbo.StudyNotes AS note
INNER JOIN LeafChoices AS leaf
    ON leaf.AncestorId = note.SubjectId
   AND leaf.ChoiceOrder = 1
INNER JOIN dbo.Subjects AS sourceSubject ON sourceSubject.Id = note.SubjectId
INNER JOIN dbo.Topics AS topic ON topic.Id = note.TopicId AND topic.SubjectId = note.SubjectId
WHERE EXISTS
(
    SELECT 1
    FROM dbo.Subjects AS child
    WHERE child.ParentSubjectId = sourceSubject.Id
)
OPTION (MAXRECURSION 100);

CREATE TABLE #TopicMigrations
(
    SourceSubjectId UNIQUEIDENTIFIER NOT NULL,
    SourceTopicId UNIQUEIDENTIFIER NOT NULL,
    TargetSubjectId UNIQUEIDENTIFIER NOT NULL,
    TargetTopicId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_TopicMigrations PRIMARY KEY (SourceSubjectId, SourceTopicId, TargetSubjectId)
);

INSERT INTO #TopicMigrations
(
    SourceSubjectId,
    SourceTopicId,
    TargetSubjectId,
    TargetTopicId
)
SELECT DISTINCT
    conflict.SourceSubjectId,
    conflict.SourceTopicId,
    conflict.TargetSubjectId,
    COALESCE
    (
        (
            SELECT TOP (1) targetTopic.Id
            FROM dbo.Topics AS targetTopic
            WHERE targetTopic.SubjectId = conflict.TargetSubjectId
              AND targetTopic.Name = conflict.TopicName
            ORDER BY targetTopic.Id
        ),
        NEWID()
    )
FROM #SubjectNoteConflicts AS conflict;

INSERT INTO dbo.Topics (Id, Name, SubjectId)
SELECT
    migration.TargetTopicId,
    sourceTopic.Name,
    migration.TargetSubjectId
FROM #TopicMigrations AS migration
INNER JOIN dbo.Topics AS sourceTopic
    ON sourceTopic.Id = migration.SourceTopicId
   AND sourceTopic.SubjectId = migration.SourceSubjectId
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Topics AS targetTopic
    WHERE targetTopic.Id = migration.TargetTopicId
);

INSERT INTO dbo.SubjectNoteOwnershipMigrations
(
    MigrationKey,
    StudyNoteId,
    SourceSubjectId,
    SourceTopicId,
    TargetSubjectId,
    TargetTopicId,
    MigratedAtUtc
)
SELECT
    @MigrationKey,
    conflict.StudyNoteId,
    conflict.SourceSubjectId,
    conflict.SourceTopicId,
    conflict.TargetSubjectId,
    migration.TargetTopicId,
    SYSUTCDATETIME()
FROM #SubjectNoteConflicts AS conflict
INNER JOIN #TopicMigrations AS migration
    ON migration.SourceSubjectId = conflict.SourceSubjectId
   AND migration.SourceTopicId = conflict.SourceTopicId
   AND migration.TargetSubjectId = conflict.TargetSubjectId;

UPDATE note
SET
    note.SubjectId = conflict.TargetSubjectId,
    note.TopicId = migration.TargetTopicId
FROM dbo.StudyNotes AS note
INNER JOIN #SubjectNoteConflicts AS conflict ON conflict.StudyNoteId = note.Id
INNER JOIN #TopicMigrations AS migration
    ON migration.SourceSubjectId = conflict.SourceSubjectId
   AND migration.SourceTopicId = conflict.SourceTopicId
   AND migration.TargetSubjectId = conflict.TargetSubjectId;

EXEC(N'
CREATE TRIGGER dbo.TR_StudyNotes_LeafSubjectOnly
ON dbo.StudyNotes
AFTER INSERT, UPDATE
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
        THROW 51001, ''Study notes can only belong to leaf subjects.'', 1;
END;
');

EXEC(N'
CREATE TRIGGER dbo.TR_Subjects_LeafNoteOwnership
ON dbo.Subjects
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Subjects AS subject
        WHERE EXISTS (SELECT 1 FROM dbo.StudyNotes AS note WHERE note.SubjectId = subject.Id)
          AND EXISTS (SELECT 1 FROM dbo.Subjects AS child WHERE child.ParentSubjectId = subject.Id)
    )
        THROW 51002, ''A subject with study notes cannot have children.'', 1;

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
