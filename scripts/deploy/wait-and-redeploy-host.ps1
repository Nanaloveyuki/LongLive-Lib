param(
    [string]$Configuration = 'Debug',
    [string]$PluginsDir = '',
    [int]$TimeoutSeconds = 900,
    [int]$PollSeconds = 5,
    [switch]$SkipRuntimeCheck,
    [switch]$SkipNative
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$propsPath = Join-Path $repoRoot 'eng\LocalReferences.props'
$redeployScript = Join-Path $PSScriptRoot 'redeploy-host.ps1'

if (-not (Test-Path $propsPath)) {
    throw 'Missing eng/LocalReferences.props. Copy eng/LocalReferences.props.example first.'
}

if (-not (Test-Path $redeployScript)) {
    throw 'Missing scripts/redeploy-host.ps1'
}

[xml]$propsXml = [System.IO.File]::ReadAllText($propsPath, [System.Text.Encoding]::UTF8)
$propertyGroup = $propsXml.Project.PropertyGroup
$gameRoot = [string]$propertyGroup.McsGameRoot

if ([string]::IsNullOrWhiteSpace($gameRoot)) {
    throw 'McsGameRoot is empty in eng/LocalReferences.props.'
}

if ($TimeoutSeconds -lt 1) {
    throw 'TimeoutSeconds must be >= 1.'
}

if ($PollSeconds -lt 1) {
    throw 'PollSeconds must be >= 1.'
}

function Get-RunningGameProcesses {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedGameRoot
    )

    return Get-CimInstance Win32_Process | Where-Object {
        $_.ExecutablePath -like (Join-Path $ResolvedGameRoot '*')
    } | Select-Object Name, ProcessId, ExecutablePath, CreationDate
}

Write-Host 'LongLive wait-and-redeploy host'
Write-Host "Configuration   : $Configuration"
Write-Host "PluginsDir      : $(if ([string]::IsNullOrWhiteSpace($PluginsDir)) { '<auto>' } else { $PluginsDir })"
Write-Host "TimeoutSeconds  : $TimeoutSeconds"
Write-Host "PollSeconds     : $PollSeconds"
Write-Host "SkipRuntimeCheck: $SkipRuntimeCheck"
Write-Host "SkipNative      : $SkipNative"
Write-Host ''

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)

while ($true) {
    $runningGameProcesses = Get-RunningGameProcesses -ResolvedGameRoot $gameRoot
    if (-not $runningGameProcesses -or $runningGameProcesses.Count -eq 0) {
        Write-Host 'No running game-root processes detected. Starting guarded redeploy.'
        break
    }

    if ((Get-Date) -ge $deadline) {
        $processSummary = ($runningGameProcesses | ForEach-Object { "$($_.Name)#$($_.ProcessId)" }) -join ', '
        throw "Timed out waiting for game-root processes to exit: $processSummary"
    }

    $processSummary = ($runningGameProcesses | ForEach-Object { "$($_.Name)#$($_.ProcessId)" }) -join ', '
    Write-Warning "Still waiting for game-root processes to exit: $processSummary"
    Start-Sleep -Seconds $PollSeconds
}

if ([string]::IsNullOrWhiteSpace($PluginsDir)) {
    if ($SkipRuntimeCheck) {
        if ($SkipNative) {
            & $redeployScript -Configuration $Configuration -SkipRuntimeCheck -SkipNative
        }
        else {
            & $redeployScript -Configuration $Configuration -SkipRuntimeCheck
        }
    }
    else {
        if ($SkipNative) {
            & $redeployScript -Configuration $Configuration -SkipNative
        }
        else {
            & $redeployScript -Configuration $Configuration
        }
    }
}
else {
    if ($SkipRuntimeCheck) {
        if ($SkipNative) {
            & $redeployScript -Configuration $Configuration -PluginsDir $PluginsDir -SkipRuntimeCheck -SkipNative
        }
        else {
            & $redeployScript -Configuration $Configuration -PluginsDir $PluginsDir -SkipRuntimeCheck
        }
    }
    else {
        if ($SkipNative) {
            & $redeployScript -Configuration $Configuration -PluginsDir $PluginsDir -SkipNative
        }
        else {
            & $redeployScript -Configuration $Configuration -PluginsDir $PluginsDir
        }
    }
}

if ($LASTEXITCODE -ne 0) {
    throw "redeploy-host.ps1 failed with exit code $LASTEXITCODE."
}
