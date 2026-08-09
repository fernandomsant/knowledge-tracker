function Stop-KnowledgeTrackerBackend {
    $backendProcesses = @(Get-Process -Name 'KnowledgeTracker.Web' -ErrorAction SilentlyContinue)
    if ($backendProcesses.Count -eq 0) {
        return
    }

    Write-Output 'Stopping the previously running backend...'
    foreach ($backendProcess in $backendProcesses) {
        Stop-Process -Id $backendProcess.Id -Force
        Wait-Process -Id $backendProcess.Id -Timeout 10 -ErrorAction SilentlyContinue
    }
}
