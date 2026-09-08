CREATE TABLE dbo.Workspaces
(
    Id UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(256) NOT NULL,
    CreatedAtUtc DATETIMEOFFSET(7) NOT NULL,
    CONSTRAINT PK_Workspaces PRIMARY KEY (Id),
    CONSTRAINT UX_Workspaces_Id_UserId UNIQUE (Id, UserId),
    CONSTRAINT UX_Workspaces_UserId_Name UNIQUE (UserId, Name),
    CONSTRAINT FK_Workspaces_Users_UserId
        FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
    CONSTRAINT CK_Workspaces_NameNotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);

CREATE INDEX IX_Workspaces_UserId_CreatedAtUtc
    ON dbo.Workspaces (UserId, CreatedAtUtc, Id);

INSERT INTO dbo.Workspaces (Id, UserId, Name, CreatedAtUtc)
SELECT
    CONVERT(UNIQUEIDENTIFIER, SUBSTRING(HASHBYTES('MD5', CONCAT('knowledge-tracker:default-workspace:', CONVERT(VARCHAR(36), userRecord.Id))), 1, 16)),
    userRecord.Id,
    'Personal',
    CONVERT(DATETIMEOFFSET(7), '2000-01-01T00:00:00+00:00')
FROM dbo.Users AS userRecord
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Workspaces AS workspace
    WHERE workspace.UserId = userRecord.Id
);

ALTER TABLE dbo.Subjects ADD WorkspaceId UNIQUEIDENTIFIER NULL;
ALTER TABLE dbo.StudyMetricDefinitions ADD UserId UNIQUEIDENTIFIER NULL;

EXEC(N'
UPDATE subject
SET WorkspaceId = workspace.Id
FROM dbo.Subjects AS subject
INNER JOIN dbo.Workspaces AS workspace
    ON workspace.UserId = subject.UserId
   AND workspace.Name = ''Personal'';

IF EXISTS (SELECT 1 FROM dbo.Subjects WHERE WorkspaceId IS NULL)
    THROW 50002, ''Cannot assign existing subjects to a workspace.'', 1;

ALTER TABLE dbo.Subjects ALTER COLUMN WorkspaceId UNIQUEIDENTIFIER NOT NULL;

ALTER TABLE dbo.Subjects
    ADD CONSTRAINT FK_Subjects_Workspaces_WorkspaceId_UserId
    FOREIGN KEY (WorkspaceId, UserId) REFERENCES dbo.Workspaces (Id, UserId);

CREATE INDEX IX_Subjects_UserId_WorkspaceId_Name
    ON dbo.Subjects (UserId, WorkspaceId, Name, Id);

DECLARE @PrimaryUserId UNIQUEIDENTIFIER =
(
    SELECT MIN(Id)
    FROM dbo.Users
);

ALTER TABLE dbo.StudyMetricDefinitions
    DROP CONSTRAINT UX_StudyMetricDefinitions_NormalizedName;

IF @PrimaryUserId IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.StudyNoteMetrics)
        THROW 50003, ''Cannot scope study metrics because study note metrics exist without users.'', 1;
    IF EXISTS (SELECT 1 FROM dbo.SubjectGoals WHERE MetricDefinitionId IS NOT NULL)
        THROW 50004, ''Cannot scope study metrics because subject goals exist without users.'', 1;

    DELETE FROM dbo.StudyMetricDefinitions;
END
ELSE
BEGIN
    CREATE TABLE #MetricDefinitionMap
    (
        SourceDefinitionId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        TargetDefinitionId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_MetricDefinitionMap PRIMARY KEY (SourceDefinitionId, UserId),
        CONSTRAINT UX_MetricDefinitionMap_TargetDefinitionId UNIQUE (TargetDefinitionId)
    );

    INSERT INTO #MetricDefinitionMap (SourceDefinitionId, UserId, TargetDefinitionId)
    SELECT
        definition.Id,
        userRecord.Id,
        CASE
            WHEN userRecord.Id = @PrimaryUserId THEN definition.Id
            ELSE CONVERT(UNIQUEIDENTIFIER, SUBSTRING(HASHBYTES(''MD5'', CONCAT(''knowledge-tracker:metric-definition:'', CONVERT(VARCHAR(36), definition.Id), '':'', CONVERT(VARCHAR(36), userRecord.Id))), 1, 16))
        END
    FROM dbo.StudyMetricDefinitions AS definition
    CROSS JOIN dbo.Users AS userRecord;

    INSERT INTO dbo.StudyMetricDefinitions (Id, UserId, Name, NormalizedName, NumberKind)
    SELECT
        mapping.TargetDefinitionId,
        mapping.UserId,
        definition.Name,
        definition.NormalizedName,
        definition.NumberKind
    FROM #MetricDefinitionMap AS mapping
    INNER JOIN dbo.StudyMetricDefinitions AS definition
        ON definition.Id = mapping.SourceDefinitionId
    WHERE mapping.TargetDefinitionId <> mapping.SourceDefinitionId;

    UPDATE metric
    SET MetricDefinitionId = mapping.TargetDefinitionId
    FROM dbo.StudyNoteMetrics AS metric
    INNER JOIN dbo.StudyNotes AS note ON note.Id = metric.StudyNoteId
    INNER JOIN dbo.Subjects AS subject ON subject.Id = note.SubjectId
    INNER JOIN #MetricDefinitionMap AS mapping
        ON mapping.SourceDefinitionId = metric.MetricDefinitionId
       AND mapping.UserId = subject.UserId;

    UPDATE goal
    SET MetricDefinitionId = mapping.TargetDefinitionId
    FROM dbo.SubjectGoals AS goal
    INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId
    INNER JOIN #MetricDefinitionMap AS mapping
        ON mapping.SourceDefinitionId = goal.MetricDefinitionId
       AND mapping.UserId = subject.UserId;

    UPDATE definition
    SET UserId = @PrimaryUserId
    FROM dbo.StudyMetricDefinitions AS definition
    WHERE definition.UserId IS NULL;
END;

ALTER TABLE dbo.StudyMetricDefinitions ALTER COLUMN UserId UNIQUEIDENTIFIER NOT NULL;

ALTER TABLE dbo.StudyMetricDefinitions
    ADD CONSTRAINT FK_StudyMetricDefinitions_Users_UserId
    FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE;

ALTER TABLE dbo.StudyMetricDefinitions
    ADD CONSTRAINT UX_StudyMetricDefinitions_UserId_NormalizedName
    UNIQUE (UserId, NormalizedName);
');
