INSERT INTO dbo.StudyMetricDefinitions (Id, Name, NormalizedName, NumberKind)
SELECT
    'A0D2E2F1-9C18-4D4B-9D01-6B2A7CB4C520',
    'Study time',
    'STUDY TIME',
    2
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.StudyMetricDefinitions
    WHERE NormalizedName = 'STUDY TIME'
);
