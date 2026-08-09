$workspaceRoot = Split-Path -Parent $PSScriptRoot
$processFile = Join-Path $workspaceRoot '.dev/processes.json'
. (Join-Path $PSScriptRoot 'DevelopmentProcess.ps1')

if (-not (Test-Path -LiteralPath $processFile)) {
    Write-Output 'No development services are tracked.'
    Stop-KnowledgeTrackerBackend
    exit 0
}

$processes = Get-Content -Raw -LiteralPath $processFile | ConvertFrom-Json
foreach ($process in $processes) {
    & taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null
}

Stop-KnowledgeTrackerBackend
Remove-Item -LiteralPath $processFile -Force
Write-Output 'Development services stopped.'
