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

Write-Output 'Building the .NET solution...'
& dotnet build 'src/KnowledgeTracker/KnowledgeTracker.slnx' --no-restore -m:1 --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    throw 'The .NET build failed using the existing restore cache. Run dotnet restore once after changing package references.'
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

    Save-TrackedProcesses -Services $services

    Write-Output 'Backend:  http://localhost:5015'
    Write-Output 'Frontend: http://localhost:5173'
    Write-Output 'Press Ctrl+C to stop both services.'

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
