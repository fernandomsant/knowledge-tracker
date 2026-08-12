$workspaceRoot = Split-Path -Parent $PSScriptRoot

& dotnet run --project (Join-Path $workspaceRoot 'src\KnowledgeTracker\KnowledgeTracker.Seeding') -- $args
exit $LASTEXITCODE
