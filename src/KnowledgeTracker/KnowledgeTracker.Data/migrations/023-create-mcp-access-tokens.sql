CREATE TABLE dbo.McpAccessTokens
(
    Id UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(256) NOT NULL,
    TokenIdentifier VARCHAR(128) NOT NULL,
    SecretHash NVARCHAR(1024) NOT NULL,
    CreatedAtUtc DATETIMEOFFSET(7) NOT NULL,
    ExpiresAtUtc DATETIMEOFFSET(7) NOT NULL,
    RevokedAtUtc DATETIMEOFFSET(7) NULL,
    LastUsedAtUtc DATETIMEOFFSET(7) NULL,
    CONSTRAINT PK_McpAccessTokens PRIMARY KEY (Id),
    CONSTRAINT FK_McpAccessTokens_Users_UserId
        FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
    CONSTRAINT UX_McpAccessTokens_TokenIdentifier UNIQUE (TokenIdentifier),
    CONSTRAINT CK_McpAccessTokens_Name CHECK (LEN(LTRIM(RTRIM(Name))) BETWEEN 1 AND 256),
    CONSTRAINT CK_McpAccessTokens_TokenIdentifier CHECK (LEN(TokenIdentifier) BETWEEN 1 AND 128),
    CONSTRAINT CK_McpAccessTokens_SecretHash CHECK (LEN(SecretHash) > 0),
    CONSTRAINT CK_McpAccessTokens_ExpiresAfterCreation CHECK (ExpiresAtUtc > CreatedAtUtc),
    CONSTRAINT CK_McpAccessTokens_RevokedAfterCreation CHECK (RevokedAtUtc IS NULL OR RevokedAtUtc >= CreatedAtUtc),
    CONSTRAINT CK_McpAccessTokens_LastUsedAfterCreation CHECK (LastUsedAtUtc IS NULL OR LastUsedAtUtc >= CreatedAtUtc)
);

CREATE INDEX IX_McpAccessTokens_User_CreatedAtUtc
    ON dbo.McpAccessTokens (UserId, CreatedAtUtc DESC, Id);

CREATE TABLE dbo.McpAccessTokenScopes
(
    McpAccessTokenId UNIQUEIDENTIFIER NOT NULL,
    Scope VARCHAR(128) NOT NULL,
    CONSTRAINT PK_McpAccessTokenScopes PRIMARY KEY (McpAccessTokenId, Scope),
    CONSTRAINT FK_McpAccessTokenScopes_McpAccessTokens_McpAccessTokenId
        FOREIGN KEY (McpAccessTokenId) REFERENCES dbo.McpAccessTokens (Id) ON DELETE CASCADE,
    CONSTRAINT CK_McpAccessTokenScopes_Scope CHECK (LEN(Scope) BETWEEN 1 AND 128)
);

CREATE INDEX IX_McpAccessTokenScopes_Scope
    ON dbo.McpAccessTokenScopes (Scope, McpAccessTokenId);
