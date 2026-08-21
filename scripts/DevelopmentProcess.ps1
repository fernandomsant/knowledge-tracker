function Stop-KnowledgeTrackerBackend {
    $backendProcesses = @(
        Get-Process -Name 'KnowledgeTracker.Web', 'KnowledgeTracker.ClassificationWorker' -ErrorAction SilentlyContinue
    )
    if ($backendProcesses.Count -eq 0) {
        return
    }

    Write-Output 'Stopping previously running backend services...'
    foreach ($backendProcess in $backendProcesses) {
        Stop-Process -Id $backendProcess.Id -Force
        Wait-Process -Id $backendProcess.Id -Timeout 10 -ErrorAction SilentlyContinue
    }
}
