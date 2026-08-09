$workspaceRoot = Split-Path -Parent $PSScriptRoot
$runtimeDirectory = Join-Path $workspaceRoot '.dev'
$processFile = Join-Path $runtimeDirectory 'processes.json'
. (Join-Path $PSScriptRoot 'DevelopmentProcess.ps1')

if (Test-Path -LiteralPath $processFile) {
    $processes = Get-Content -Raw -LiteralPath $processFile | ConvertFrom-Json
    $running = @($processes | Where-Object { Get-Process -Id $_.Id -ErrorAction SilentlyContinue })
    if ($running.Count -gt 0) {
        throw 'The development services are already running. Use npm run dev:stop before starting them again.'
    }
}

New-Item -ItemType Directory -Force -Path $runtimeDirectory | Out-Null

Stop-KnowledgeTrackerBackend
& dotnet build 'src/KnowledgeTracker/KnowledgeTracker.slnx' --no-restore -m:1 --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    throw 'The backend build failed. Development services were not started.'
}

$backend = Start-Process -FilePath 'dotnet' `
    -ArgumentList @('run', '--no-build', '--project', 'src/KnowledgeTracker/KnowledgeTracker.Web', '--launch-profile', 'http') `
    -WorkingDirectory $workspaceRoot `
    -RedirectStandardOutput (Join-Path $runtimeDirectory 'backend.log') `
    -RedirectStandardError (Join-Path $runtimeDirectory 'backend.error.log') `
    -WindowStyle Hidden `
    -PassThru

$frontend = Start-Process -FilePath 'npm.cmd' `
    -ArgumentList @('run', 'dev', '--', '--host', '127.0.0.1', '--strictPort') `
    -WorkingDirectory (Join-Path $workspaceRoot 'src/frontend') `
    -RedirectStandardOutput (Join-Path $runtimeDirectory 'frontend.log') `
    -RedirectStandardError (Join-Path $runtimeDirectory 'frontend.error.log') `
    -WindowStyle Hidden `
    -PassThru

@(
    [pscustomobject]@{ Name = 'backend'; Id = $backend.Id },
    [pscustomobject]@{ Name = 'frontend'; Id = $frontend.Id }
) | ConvertTo-Json | Set-Content -LiteralPath $processFile -Encoding utf8

Write-Output 'Backend:  http://localhost:5015'
Write-Output 'Frontend: http://127.0.0.1:5173'
Write-Output 'Logs:     .dev/'
