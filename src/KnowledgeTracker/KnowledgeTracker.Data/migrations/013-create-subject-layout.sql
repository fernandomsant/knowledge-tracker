CREATE TABLE dbo.SubjectLayout
(
    SubjectId UNIQUEIDENTIFIER NOT NULL,
    NormalizedX DECIMAL(9,8) NOT NULL,
    NormalizedY DECIMAL(9,8) NOT NULL,
    UpdatedAtUtc DATETIME2(7) NOT NULL CONSTRAINT DF_SubjectLayout_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_SubjectLayout PRIMARY KEY (SubjectId),
    CONSTRAINT FK_SubjectLayout_Subjects_SubjectId FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects (Id) ON DELETE CASCADE,
    CONSTRAINT CK_SubjectLayout_NormalizedX CHECK (NormalizedX >= 0 AND NormalizedX <= 1),
    CONSTRAINT CK_SubjectLayout_NormalizedY CHECK (NormalizedY >= 0 AND NormalizedY <= 1)
);
