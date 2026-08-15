IF EXISTS (SELECT 1 FROM dbo.Topics) AND NOT EXISTS (SELECT 1 FROM dbo.Subjects)
    THROW 50000, 'Topics cannot be scoped because no subjects exist.', 1;

CREATE TABLE #TopicScopes
(
    SourceTopicId UNIQUEIDENTIFIER NOT NULL,
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    TargetTopicId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_TopicScopes PRIMARY KEY (SourceTopicId, SubjectId)
);

;WITH CandidateScopes AS
(
    SELECT TopicId, SubjectId FROM dbo.StudyNotes
    UNION
    SELECT TopicId, SubjectId FROM dbo.SubjectGoals
    UNION
    SELECT topic.Id, subject.Id
    FROM dbo.Topics AS topic
    INNER JOIN dbo.Subjects AS subject ON subject.Id = topic.Id
),
FallbackScopes AS
(
    SELECT topic.Id AS TopicId, fallbackSubject.Id AS SubjectId
    FROM dbo.Topics AS topic
    CROSS APPLY
    (
        SELECT TOP (1) Id
        FROM dbo.Subjects
        ORDER BY Id
    ) AS fallbackSubject
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM CandidateScopes AS candidate
        WHERE candidate.TopicId = topic.Id
    )
)
INSERT INTO #TopicScopes (SourceTopicId, SubjectId, TargetTopicId)
SELECT TopicId, SubjectId, NEWID()
FROM CandidateScopes
UNION
SELECT TopicId, SubjectId, NEWID()
FROM FallbackScopes;

;WITH RankedScopes AS
(
    SELECT
        SourceTopicId,
        SubjectId,
        TargetTopicId,
        ROW_NUMBER() OVER
        (
            PARTITION BY SourceTopicId
            ORDER BY CASE WHEN SourceTopicId = SubjectId THEN 0 ELSE 1 END, SubjectId
        ) AS ScopeOrder
    FROM #TopicScopes
)
UPDATE scope
SET TargetTopicId = CASE
    WHEN ranked.ScopeOrder = 1 THEN scope.SourceTopicId
    ELSE NEWID()
END
FROM #TopicScopes AS scope
INNER JOIN RankedScopes AS ranked
    ON ranked.SourceTopicId = scope.SourceTopicId
    AND ranked.SubjectId = scope.SubjectId;

ALTER TABLE dbo.Topics ADD SubjectId UNIQUEIDENTIFIER NULL;

UPDATE topic
SET SubjectId = scope.SubjectId
FROM dbo.Topics AS topic
INNER JOIN #TopicScopes AS scope
    ON scope.SourceTopicId = topic.Id
    AND scope.TargetTopicId = topic.Id;

INSERT INTO dbo.Topics (Id, Name, SubjectId)
SELECT scope.TargetTopicId, topic.Name, scope.SubjectId
FROM #TopicScopes AS scope
INNER JOIN dbo.Topics AS topic ON topic.Id = scope.SourceTopicId
WHERE scope.TargetTopicId <> scope.SourceTopicId;

UPDATE note
SET TopicId = scope.TargetTopicId
FROM dbo.StudyNotes AS note
INNER JOIN #TopicScopes AS scope
    ON scope.SourceTopicId = note.TopicId
    AND scope.SubjectId = note.SubjectId;

UPDATE goal
SET TopicId = scope.TargetTopicId
FROM dbo.SubjectGoals AS goal
INNER JOIN #TopicScopes AS scope
    ON scope.SourceTopicId = goal.TopicId
    AND scope.SubjectId = goal.SubjectId;

ALTER TABLE dbo.StudyNotes DROP CONSTRAINT FK_StudyNotes_Topics_TopicId;
ALTER TABLE dbo.SubjectGoals DROP CONSTRAINT FK_SubjectGoals_Topics_TopicId;
ALTER TABLE dbo.Topics ALTER COLUMN SubjectId UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE dbo.Topics ADD CONSTRAINT FK_Topics_Subjects_SubjectId
    FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects (Id) ON DELETE CASCADE;
ALTER TABLE dbo.Topics ADD CONSTRAINT UQ_Topics_Id_SubjectId UNIQUE (Id, SubjectId);
ALTER TABLE dbo.StudyNotes ADD CONSTRAINT FK_StudyNotes_Topics_SubjectScope
    FOREIGN KEY (TopicId, SubjectId) REFERENCES dbo.Topics (Id, SubjectId);
ALTER TABLE dbo.SubjectGoals ADD CONSTRAINT FK_SubjectGoals_Topics_SubjectScope
    FOREIGN KEY (TopicId, SubjectId) REFERENCES dbo.Topics (Id, SubjectId);
CREATE INDEX IX_Topics_SubjectId_Name ON dbo.Topics (SubjectId, Name, Id);
