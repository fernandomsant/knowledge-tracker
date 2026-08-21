$workspaceRoot = Split-Path -Parent $PSScriptRoot
$runtimeDirectory = Join-Path $workspaceRoot '.dev'
$processFile = Join-Path $runtimeDirectory 'processes.json'
. (Join-Path $PSScriptRoot 'DevelopmentProcess.ps1')

function Get-FileSha256 {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '')
    }
    finally {
        $stream.Dispose()
        $algorithm.Dispose()
    }
}

function Save-TrackedProcesses {
    param(
        [Parameter(Mandatory = $true)]
        [array] $Services
    )

    @($Services | ForEach-Object {
        [pscustomobject]@{ Name = $_.Name; Id = $_.Process.Id }
    }) | ConvertTo-Json | Set-Content -LiteralPath $processFile -Encoding utf8
}

function Initialize-FrontendDependencies {
    $frontendDirectory = Join-Path $workspaceRoot 'src/frontend'
    $lockFile = Join-Path $frontendDirectory 'package-lock.json'
    $nodeModules = Join-Path $frontendDirectory 'node_modules'
    $stampFile = Join-Path $runtimeDirectory 'frontend-dependencies.sha256'
    $lockHash = Get-FileSha256 -Path $lockFile
    $installedHash = if (Test-Path -LiteralPath $stampFile) {
        $stampContent = Get-Content -Raw -LiteralPath $stampFile
        if ([string]::IsNullOrWhiteSpace($stampContent)) { $null } else { $stampContent.Trim() }
    }
    else {
        $null
    }

    $needsInstall = -not (Test-Path -LiteralPath $nodeModules) -or (
        $null -ne $installedHash -and $installedHash -ne $lockHash
    )

    if (-not $needsInstall -and $null -eq $installedHash) {
        Push-Location $frontendDirectory
        try {
            & npm.cmd ls --depth=0 --silent *> $null
            $needsInstall = $LASTEXITCODE -ne 0
        }
        finally {
            Pop-Location
        }
    }

    if ($needsInstall) {
        Write-Output 'Installing frontend dependencies...'
        Push-Location $frontendDirectory
        try {
            & npm.cmd ci --no-audit --no-fund
            if ($LASTEXITCODE -ne 0) {
                throw 'Frontend dependency installation failed.'
            }
        }
        finally {
            Pop-Location
        }
    }

    Set-Content -LiteralPath $stampFile -Value $lockHash -Encoding ascii
}

function Initialize-ClassifierEnvironment {
    $environmentDirectory = Join-Path $runtimeDirectory 'classifier-venv'
    $classifierPython = Join-Path $environmentDirectory 'Scripts/python.exe'
    $requirementsFile = Join-Path $workspaceRoot 'src/classification-service/requirements.txt'
    $stampFile = Join-Path $environmentDirectory 'requirements.sha256'

    if (-not (Test-Path -LiteralPath $classifierPython)) {
        Write-Output 'Creating the Python classifier environment...'
        $environmentCreated = $false
        $pythonLauncher = Get-Command 'py.exe' -ErrorAction SilentlyContinue
        if ($null -ne $pythonLauncher) {
            & $pythonLauncher.Source -3 -m venv $environmentDirectory
            $environmentCreated = $LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath $classifierPython)
        }

        if (-not $environmentCreated) {
            $python = Get-Command 'python.exe' -ErrorAction SilentlyContinue
            if ($null -eq $python) {
                throw 'Python 3 is required to run the note classifier.'
            }

            & $python.Source -m venv $environmentDirectory
            $environmentCreated = $LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath $classifierPython)
        }

        if (-not $environmentCreated) {
            throw 'The Python classifier environment could not be created.'
        }
    }

    $requirementsHash = Get-FileSha256 -Path $requirementsFile
    $installedHash = if (Test-Path -LiteralPath $stampFile) {
        $stampContent = Get-Content -Raw -LiteralPath $stampFile
        if ([string]::IsNullOrWhiteSpace($stampContent)) { $null } else { $stampContent.Trim() }
    }
    else {
        $null
    }

    if ($installedHash -ne $requirementsHash) {
        Write-Output 'Updating the Python package installer...'
        & $classifierPython -m pip install --disable-pip-version-check 'pip==26.2.1'
        if ($LASTEXITCODE -ne 0) {
            throw 'The Python package installer could not be updated.'
        }

        Write-Output 'Installing Python classifier dependencies...'
        & $classifierPython -m pip install --disable-pip-version-check --prefer-binary --requirement $requirementsFile
        if ($LASTEXITCODE -ne 0) {
            throw 'Python classifier dependency installation failed.'
        }

        Set-Content -LiteralPath $stampFile -Value $requirementsHash -Encoding ascii
    }

}

function Wait-ClassifierReady {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process] $ClassifierProcess,

        [Parameter(Mandatory = $true)]
        [array] $Services
    )

    $deadline = [DateTime]::UtcNow.AddMinutes(20)
    Write-Output 'Loading the classification model (the first run may take several minutes)...'

    while ([DateTime]::UtcNow -lt $deadline) {
        foreach ($service in $Services) {
            $service.Process.Refresh()
            if ($service.Process.HasExited) {
                throw "The $($service.Name) service exited before startup completed."
            }
        }

        try {
            $health = Invoke-RestMethod -Uri 'http://127.0.0.1:8021/health' -TimeoutSec 2
            if ($health.status -eq 'ready') {
                Write-Output 'Classifier: ready'
                return
            }
        }
        catch {
            # The HTTP endpoint is unavailable while the model is loading.
        }

        Start-Sleep -Seconds 2
    }

    $ClassifierProcess.Refresh()
    if ($ClassifierProcess.HasExited) {
        throw 'The classifier exited before becoming ready.'
    }

    throw 'The classifier did not become ready within 20 minutes.'
}

if (Test-Path -LiteralPath $processFile) {
    $processes = Get-Content -Raw -LiteralPath $processFile | ConvertFrom-Json
    $running = @($processes | Where-Object { Get-Process -Id $_.Id -ErrorAction SilentlyContinue })
    if ($running.Count -gt 0) {
        Write-Output 'Restarting the existing development services...'
        foreach ($process in $running) {
            & taskkill.exe /PID $process.Id /T /F 2>$null | Out-Null
        }
    }

    Remove-Item -LiteralPath $processFile -Force
}

New-Item -ItemType Directory -Force -Path $runtimeDirectory | Out-Null

Stop-KnowledgeTrackerBackend
Initialize-FrontendDependencies
Initialize-ClassifierEnvironment
$classifierPython = Join-Path $runtimeDirectory 'classifier-venv/Scripts/python.exe'

Write-Output 'Building the .NET solution...'
& dotnet build 'src/KnowledgeTracker/KnowledgeTracker.slnx' -m:1 --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    throw 'The .NET build failed. Development services were not started.'
}

Write-Output 'Applying database migrations...'
& dotnet run --no-build --no-restore --project 'src/KnowledgeTracker/KnowledgeTracker.Migrations'
if ($LASTEXITCODE -ne 0) {
    throw 'Database migration failed. Development services were not started.'
}

$services = @()
try {
    $backend = Start-Process -FilePath 'dotnet' `
        -ArgumentList @('run', '--no-build', '--no-restore', '--project', 'src/KnowledgeTracker/KnowledgeTracker.Web', '--launch-profile', 'http') `
        -WorkingDirectory $workspaceRoot `
        -NoNewWindow `
        -PassThru
    $services += [pscustomobject]@{ Name = 'backend'; Process = $backend }

    $frontend = Start-Process -FilePath 'npm.cmd' `
        -ArgumentList @('run', 'dev', '--', '--host', 'localhost', '--strictPort') `
        -WorkingDirectory (Join-Path $workspaceRoot 'src/frontend') `
        -NoNewWindow `
        -PassThru
    $services += [pscustomobject]@{ Name = 'frontend'; Process = $frontend }

    $classifier = Start-Process -FilePath $classifierPython `
        -ArgumentList @('-m', 'uvicorn', 'app:app', '--app-dir', 'src/classification-service', '--host', '127.0.0.1', '--port', '8021') `
        -WorkingDirectory $workspaceRoot `
        -NoNewWindow `
        -PassThru
    $services += [pscustomobject]@{ Name = 'classifier'; Process = $classifier }
    Save-TrackedProcesses -Services $services

    Wait-ClassifierReady -ClassifierProcess $classifier -Services $services

    $classificationWorker = Start-Process -FilePath 'dotnet' `
        -ArgumentList @('run', '--no-build', '--no-restore', '--project', 'src/KnowledgeTracker/KnowledgeTracker.ClassificationWorker') `
        -WorkingDirectory $workspaceRoot `
        -NoNewWindow `
        -PassThru
    $services += [pscustomobject]@{ Name = 'classification worker'; Process = $classificationWorker }
    Save-TrackedProcesses -Services $services

    Write-Output 'Backend:            http://localhost:5015'
    Write-Output 'Frontend:           http://localhost:5173'
    Write-Output 'Classifier health:  http://localhost:8021/health'
    Write-Output 'Press Ctrl+C to stop all services.'

    while ($true) {
        foreach ($service in $services) {
            $service.Process.Refresh()
            if ($service.Process.HasExited) {
                throw "The $($service.Name) service exited unexpectedly."
            }
        }

        Start-Sleep -Seconds 1
    }
}
finally {
    foreach ($service in $services) {
        if ($null -ne $service.Process) {
            & taskkill.exe /PID $service.Process.Id /T /F 2>$null | Out-Null
        }
    }

    Remove-Item -LiteralPath $processFile -Force -ErrorAction SilentlyContinue
}
