CREATE TABLE dbo.StudyMetricDefinitions
(
    Id UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(256) NOT NULL,
    NormalizedName NVARCHAR(256) NOT NULL,
    NumberKind TINYINT NOT NULL,
    CONSTRAINT PK_StudyMetricDefinitions PRIMARY KEY (Id),
    CONSTRAINT UX_StudyMetricDefinitions_NormalizedName UNIQUE (NormalizedName),
    CONSTRAINT CK_StudyMetricDefinitions_NameNotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
    CONSTRAINT CK_StudyMetricDefinitions_NumberKind CHECK (NumberKind IN (1, 2))
);

INSERT INTO dbo.StudyMetricDefinitions (Id, Name, NormalizedName, NumberKind)
VALUES
    ('B2B182D0-8709-4328-BDA1-0A73B51D0E82', 'Pages read', 'PAGES READ', 1),
    ('6D584D3A-6D8E-4B7A-A9AF-2C52C90DAA5E', 'Exercises done', 'EXERCISES DONE', 1);

INSERT INTO dbo.StudyMetricDefinitions (Id, Name, NormalizedName, NumberKind)
SELECT
    CONVERT(UNIQUEIDENTIFIER, SUBSTRING(HASHBYTES('MD5', metric.NormalizedName), 1, 16)),
    MAX(metric.Name),
    metric.NormalizedName,
    CASE WHEN MAX(CASE WHEN metric.MetricValue <> FLOOR(metric.MetricValue) THEN 1 ELSE 0 END) = 1 THEN 2 ELSE 1 END
FROM dbo.StudyNoteMetrics AS metric
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.StudyMetricDefinitions AS definition
    WHERE definition.NormalizedName = metric.NormalizedName
)
GROUP BY metric.NormalizedName;

ALTER TABLE dbo.StudyNoteMetrics ADD MetricDefinitionId UNIQUEIDENTIFIER NULL;

UPDATE metric
SET MetricDefinitionId = definition.Id
FROM dbo.StudyNoteMetrics AS metric
INNER JOIN dbo.StudyMetricDefinitions AS definition ON definition.NormalizedName = metric.NormalizedName;

ALTER TABLE dbo.StudyNoteMetrics ALTER COLUMN MetricDefinitionId UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE dbo.StudyNoteMetrics DROP CONSTRAINT PK_StudyNoteMetrics;
ALTER TABLE dbo.StudyNoteMetrics DROP CONSTRAINT CK_StudyNoteMetrics_NameNotBlank;
ALTER TABLE dbo.StudyNoteMetrics DROP COLUMN NormalizedName, Name;
ALTER TABLE dbo.StudyNoteMetrics
    ADD CONSTRAINT PK_StudyNoteMetrics PRIMARY KEY (StudyNoteId, MetricDefinitionId);
ALTER TABLE dbo.StudyNoteMetrics
    ADD CONSTRAINT FK_StudyNoteMetrics_StudyMetricDefinitions_MetricDefinitionId
    FOREIGN KEY (MetricDefinitionId) REFERENCES dbo.StudyMetricDefinitions (Id);
