$workspaceRoot = Split-Path -Parent $PSScriptRoot

& dotnet run --project (Join-Path $workspaceRoot 'src\KnowledgeTracker\KnowledgeTracker.Migrations') -- $args
exit $LASTEXITCODE
