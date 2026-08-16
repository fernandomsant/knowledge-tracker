UPDATE dbo.SubjectLayout
SET
    NormalizedX = ROUND(NormalizedX * CAST(33 AS DECIMAL(18, 12)) / 113, 8),
    NormalizedY = ROUND(NormalizedY * CAST(287 AS DECIMAL(18, 12)) / 1127, 8);
