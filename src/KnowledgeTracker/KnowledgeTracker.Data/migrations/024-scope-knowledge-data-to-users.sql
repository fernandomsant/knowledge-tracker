ALTER TABLE dbo.Subjects ADD UserId UNIQUEIDENTIFIER NULL;

DECLARE @LegacyUserId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) Id
    FROM dbo.Users
    ORDER BY Id
);

IF EXISTS (SELECT 1 FROM dbo.Subjects WHERE UserId IS NULL) AND @LegacyUserId IS NULL
    THROW 50001, 'Cannot assign existing subjects to a user because dbo.Users is empty.', 1;

UPDATE dbo.Subjects
SET UserId = @LegacyUserId
WHERE UserId IS NULL;

ALTER TABLE dbo.Subjects ALTER COLUMN UserId UNIQUEIDENTIFIER NOT NULL;

ALTER TABLE dbo.Subjects ADD CONSTRAINT FK_Subjects_Users_UserId
    FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE;

CREATE INDEX IX_Subjects_UserId_Name
    ON dbo.Subjects (UserId, Name, Id);
