param(
    [string]$Configuration = 'Debug',
    [string]$PluginsDir = '',
    [switch]$SkipBuild,
    [switch]$SkipRuntimeCheck,
    [switch]$SkipNative
)

$ErrorActionPreference = 'Stop'

$buildScript = Join-Path $PSScriptRoot 'build-host.ps1'
$deployScript = Join-Path $PSScriptRoot 'deploy-host.ps1'
$verifyRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'verify'
$deployCheckScript = Join-Path $verifyRoot 'check-host-deploy.ps1'
$runtimeCheckScript = Join-Path $verifyRoot 'check-host-runtime.ps1'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$propsPath = Join-Path $repoRoot 'eng\LocalReferences.props'

foreach ($path in @($buildScript, $deployScript, $deployCheckScript, $runtimeCheckScript)) {
    if (-not (Test-Path $path)) {
        throw "Missing required script: $path"
    }
}

if (-not (Test-Path $propsPath)) {
    throw 'Missing eng/LocalReferences.props. Copy eng/LocalReferences.props.example first.'
}

[xml]$propsXml = [System.IO.File]::ReadAllText($propsPath, [System.Text.Encoding]::UTF8)
$propertyGroup = $propsXml.Project.PropertyGroup
$gameRoot = [string]$propertyGroup.McsGameRoot

if ([string]::IsNullOrWhiteSpace($gameRoot)) {
    throw 'McsGameRoot is empty in eng/LocalReferences.props.'
}

Write-Host 'LongLive safe host redeploy'
Write-Host "Configuration   : $Configuration"
Write-Host "PluginsDir      : $(if ([string]::IsNullOrWhiteSpace($PluginsDir)) { '<auto>' } else { $PluginsDir })"
Write-Host "SkipBuild       : $SkipBuild"
Write-Host "SkipRuntimeCheck: $SkipRuntimeCheck"
Write-Host "SkipNative      : $SkipNative"
Write-Host ''

$runningGameProcesses = Get-CimInstance Win32_Process | Where-Object {
    $_.ExecutablePath -like (Join-Path $gameRoot '*')
}

if ($runningGameProcesses) {
    $processSummary = ($runningGameProcesses | ForEach-Object { "$($_.Name)#$($_.ProcessId)" }) -join ', '
    throw "Redeploy aborted before copy because game-root processes are still running: $processSummary"
}

if (-not $SkipBuild) {
    & $buildScript -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Host build failed with exit code $LASTEXITCODE."
    }
}

if ([string]::IsNullOrWhiteSpace($PluginsDir)) {
    if ($SkipNative) {
        & $deployScript -Configuration $Configuration -SkipNative
    }
    else {
        & $deployScript -Configuration $Configuration
    }
}
else {
    if ($SkipNative) {
        & $deployScript -Configuration $Configuration -PluginsDir $PluginsDir -SkipNative
    }
    else {
        & $deployScript -Configuration $Configuration -PluginsDir $PluginsDir
    }
}

if ($LASTEXITCODE -ne 0) {
    throw "Host deploy failed with exit code $LASTEXITCODE."
}

& $deployCheckScript -Configuration $Configuration -PluginsDir $PluginsDir
if ($LASTEXITCODE -ne 0) {
    throw "Post-deploy verification failed."
}

if (-not $SkipRuntimeCheck) {
    & $runtimeCheckScript -Configuration $Configuration -PluginsDir $PluginsDir
    if ($LASTEXITCODE -ne 0) {
        throw "Runtime verification failed."
    }
}

Write-Host ''
Write-Host 'LongLive safe host redeploy completed successfully.'
