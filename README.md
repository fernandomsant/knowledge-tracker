# Knowledge Tracker

Knowledge Tracker is a study-management application with a React frontend and an ASP.NET Core API. It lets authenticated users create subjects, connect related topics, and save study notes.

## Prerequisites

- .NET SDK 10
- Node.js and npm
- SQL Server or SQL Server LocalDB
- PowerShell on Windows

## First-time setup

From the repository root, restore both application stacks:

```powershell
dotnet restore src/KnowledgeTracker/KnowledgeTracker.slnx
npm install --prefix src/frontend
```

### Configure the backend

Create `src/KnowledgeTracker/KnowledgeTracker.Web/appsettings.Development.json` if it does not exist. Configure a SQL Server connection string under either `KnowledgeTracker` or `KnowledgeTracker_01`:

```json
{
  "ConnectionStrings": {
    "KnowledgeTracker": "Server=(localdb)\\MSSQLLocalDB;Database=KnowledgeTracker;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

The API also requires two Base64-encoded secrets, each at least 32 bytes after decoding. Store them with .NET user secrets:

```powershell
dotnet user-secrets --project src/KnowledgeTracker/KnowledgeTracker.Web set "Authentication:AccessTokenSigningKey" "<base64-secret>"
dotnet user-secrets --project src/KnowledgeTracker/KnowledgeTracker.Web set "Authentication:RefreshTokenPepper" "<base64-secret>"
```

Generate a suitable secret in PowerShell:

```powershell
$bytes = New-Object byte[] 64
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

Run the command twice—once for each secret.

## Apply database migrations

Migrations are explicit SQL files in `src/KnowledgeTracker/KnowledgeTracker.Data/migrations`. Apply unapplied migrations with:

```powershell
npm run migrate
```

This also creates the local starter workspace. Sign in with `student` / `student` to explore its seeded subjects and study notes.

The migration runner reads the development connection string, applies files in sequence, records them in `dbo.SchemaMigrations`, verifies their checksums, and locks migration execution so concurrent runs cannot conflict.

For deployment or CI, provide the connection string through an environment variable instead:

```powershell
$env:ConnectionStrings__KnowledgeTracker = "<connection string>"
dotnet run --project src/KnowledgeTracker/KnowledgeTracker.Migrations
```

## Run locally

Start the backend and frontend from the repository root:

```powershell
npm run dev
```

The command builds the solution, starts both processes in the current terminal, and stops them when you press `Ctrl+C`.

| Service | Address |
| --- | --- |
| Frontend | `http://localhost:5173` |
| Backend API | `http://localhost:5015` |

To stop processes started by the development script from another terminal:

```powershell
npm run dev:stop
```

## Useful commands

```powershell
# Build the backend
dotnet build src/KnowledgeTracker/KnowledgeTracker.slnx -m:1

# Build the frontend for production
npm run build --prefix src/frontend

# Reapply pending database migrations
npm run migrate
```

## Project structure

- `src/frontend` — React and Vite frontend
- `src/KnowledgeTracker/KnowledgeTracker.Domain` — domain model
- `src/KnowledgeTracker/KnowledgeTracker.Application` — use cases and contracts
- `src/KnowledgeTracker/KnowledgeTracker.Data` — SQL repositories and migrations
- `src/KnowledgeTracker/KnowledgeTracker.Infrastructure` — authentication and infrastructure services
- `src/KnowledgeTracker/KnowledgeTracker.Web` — ASP.NET Core HTTP API
- `src/KnowledgeTracker/KnowledgeTracker.Migrations` — executable SQL migration runner
