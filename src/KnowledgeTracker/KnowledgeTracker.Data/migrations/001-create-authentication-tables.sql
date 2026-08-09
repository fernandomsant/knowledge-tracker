CREATE TABLE dbo.Users
(
    Id UNIQUEIDENTIFIER NOT NULL,
    Login NVARCHAR(256) NOT NULL,
    NormalizedLogin NVARCHAR(256) NOT NULL,
    PasswordHash NVARCHAR(1024) NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT UX_Users_NormalizedLogin UNIQUE (NormalizedLogin)
);

CREATE TABLE dbo.AuthenticationSessions
(
    Id UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Nonce UNIQUEIDENTIFIER NOT NULL,
    AuthenticatedAtUtc DATETIMEOFFSET(7) NOT NULL,
    ExpiresAtUtc DATETIMEOFFSET(7) NOT NULL,
    UserAgent NVARCHAR(1024) NOT NULL,
    ClientIpAddress VARCHAR(45) NOT NULL,
    ClientSourcePort INT NOT NULL,
    IsRevoked BIT NOT NULL CONSTRAINT DF_AuthenticationSessions_IsRevoked DEFAULT (0),
    CONSTRAINT PK_AuthenticationSessions PRIMARY KEY (Id),
    CONSTRAINT FK_AuthenticationSessions_Users_UserId
        FOREIGN KEY (UserId) REFERENCES dbo.Users (Id),
    CONSTRAINT CK_AuthenticationSessions_ExpiresAfterAuthentication
        CHECK (ExpiresAtUtc > AuthenticatedAtUtc),
    CONSTRAINT CK_AuthenticationSessions_ClientSourcePort
        CHECK (ClientSourcePort BETWEEN 0 AND 65535)
);

CREATE INDEX IX_AuthenticationSessions_ActiveUser_AuthenticatedAtUtc
    ON dbo.AuthenticationSessions (UserId, AuthenticatedAtUtc)
    WHERE IsRevoked = 0;

CREATE TABLE dbo.RefreshTokens
(
    TokenHash BINARY(64) NOT NULL,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    Issuer NVARCHAR(256) NOT NULL,
    SubjectUserId UNIQUEIDENTIFIER NOT NULL,
    AuthenticatedAtUtc DATETIMEOFFSET(7) NOT NULL,
    ExpiresAtUtc DATETIMEOFFSET(7) NOT NULL,
    Nonce UNIQUEIDENTIFIER NOT NULL,
    ConsumedAtUtc DATETIMEOFFSET(7) NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY (TokenHash),
    CONSTRAINT FK_RefreshTokens_AuthenticationSessions_SessionId
        FOREIGN KEY (SessionId) REFERENCES dbo.AuthenticationSessions (Id) ON DELETE CASCADE,
    CONSTRAINT CK_RefreshTokens_ExpiresAfterAuthentication
        CHECK (ExpiresAtUtc > AuthenticatedAtUtc)
);

CREATE INDEX IX_RefreshTokens_ActiveSession
    ON dbo.RefreshTokens (SessionId, ExpiresAtUtc)
    WHERE ConsumedAtUtc IS NULL;