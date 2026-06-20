param(
    [string]$Configuration = 'Debug',
    [int]$Tail = 400,
    [string]$PluginsDir = '',
    [string]$LogPath = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$deployCheckPath = Join-Path $PSScriptRoot 'check-host-deploy.ps1'
$outputDllPath = Join-Path $repoRoot "src\LongLive.BepInEx\bin\$Configuration\net472\LongLive.BepInEx.dll"
$propsPath = Join-Path $repoRoot 'eng\LocalReferences.props'

if (-not (Test-Path $deployCheckPath)) {
    throw 'Missing scripts/check-host-deploy.ps1'
}

if (-not (Test-Path $outputDllPath)) {
    throw "Build output DLL not found: $outputDllPath"
}

if (-not (Test-Path $propsPath)) {
    throw 'Missing eng/LocalReferences.props. Copy eng/LocalReferences.props.example first.'
}

function Get-ConfiguredGameRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PropsPath
    )

    [xml]$propsXml = [System.IO.File]::ReadAllText($PropsPath, [System.Text.Encoding]::UTF8)
    return [string]$propsXml.Project.PropertyGroup.McsGameRoot
}

function Get-AssemblyModuleVersionId {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AssemblyPath
    )

    $stream = [System.IO.File]::Open($AssemblyPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        $assembly = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($AssemblyPath))
        return $assembly.ManifestModule.ModuleVersionId.ToString()
    }
    finally {
        $stream.Dispose()
    }
}

$expectedModuleVersionId = Get-AssemblyModuleVersionId -AssemblyPath $outputDllPath

function Resolve-HostLogPath {
    param(
        [string]$ConfiguredLogPath,
        [string]$PropsPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredLogPath)) {
        if (-not (Test-Path $ConfiguredLogPath)) {
            throw "Configured log path does not exist: $ConfiguredLogPath"
        }

        return $ConfiguredLogPath
    }

    [xml]$propsXml = [System.IO.File]::ReadAllText($PropsPath, [System.Text.Encoding]::UTF8)
    $propertyGroup = $propsXml.Project.PropertyGroup
    $gameRoot = [string]$propertyGroup.McsGameRoot
    $bepInExCoreDir = [string]$propertyGroup.BepInExCoreDir
    $coreParent = if ([string]::IsNullOrWhiteSpace($bepInExCoreDir)) { '' } else { Split-Path -Parent $bepInExCoreDir }

    $candidatePaths = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($coreParent)) {
        $candidatePaths.Add((Join-Path $coreParent 'LogOutput.log'))
    }

    if (-not [string]::IsNullOrWhiteSpace($gameRoot)) {
        $candidatePaths.Add((Join-Path $gameRoot 'BepInEx\LogOutput.log'))
    }

    $resolvedPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($resolvedPath)) {
        throw 'BepInEx LogOutput.log not found. Pass -LogPath explicitly if the runtime layout is unusual.'
    }

    return $resolvedPath
}

function Get-LatestLongLiveStartupBlock {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedLogPath,

        [Parameter(Mandatory = $true)]
        [int]$FallbackTail
    )

    $allLines = Get-Content -LiteralPath $ResolvedLogPath
    $awakePattern = 'LongLive .*plugin awake\.'
    $awakeIndexes = for ($index = 0; $index -lt $allLines.Count; $index++) {
        if ($allLines[$index] -match $awakePattern) {
            $index
        }
    }

    if ($awakeIndexes.Count -gt 0) {
        $startIndex = $awakeIndexes[$awakeIndexes.Count - 1]
        return $allLines[$startIndex..($allLines.Count - 1)]
    }

    return Get-Content -LiteralPath $ResolvedLogPath -Tail $FallbackTail
}

$resolvedLogPath = Resolve-HostLogPath -ConfiguredLogPath $LogPath -PropsPath $propsPath
$gameRoot = Get-ConfiguredGameRoot -PropsPath $propsPath
$runningGameProcesses = @()
if (-not [string]::IsNullOrWhiteSpace($gameRoot)) {
    $runningGameProcesses = Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.Path -like (Join-Path $gameRoot '*')
    }
}

$deployCheckSucceeded = $true
$deployCheckOutput = ''
try {
    $deployCheckOutput = & $deployCheckPath -Configuration $Configuration -PluginsDir $PluginsDir 2>&1 | Out-String
}
catch {
    $deployCheckSucceeded = $false
    $deployCheckOutput = ($_ | Out-String)
}

$logReadSucceeded = $true
$logReadOutput = ''
$startupBlockLineCount = 0
try {
    $startupBlock = Get-LatestLongLiveStartupBlock -ResolvedLogPath $resolvedLogPath -FallbackTail $Tail
    $startupBlockLineCount = $startupBlock.Count
    $logReadOutput = ($startupBlock | Where-Object {
        $_ -match 'LongLive feature state:' -or
        $_ -match 'LongLive host module MVID:' -or
        $_ -match 'LongLive handshake ready' -or
        $_ -match 'LongLive host bootstrap completed\.' -or
        $_ -match 'LongLive .*plugin awake\.'
    }) | Out-String
}
catch {
    $logReadSucceeded = $false
    $logReadOutput = ($_ | Out-String)
}

$hasFeatureState = $logReadOutput -match 'LongLive feature state:'
$hasMvidLine = $logReadOutput -match 'LongLive host module MVID:'
$hasHandshake = $logReadOutput -match 'LongLive handshake ready'
$hasBootstrapCompleted = $logReadOutput -match 'LongLive host bootstrap completed\.'
$hasPluginAwake = $logReadOutput -match 'plugin awake\.'
$runtimeMvid = $null
$mvidMatch = [regex]::Match($logReadOutput, 'LongLive host module MVID:\s*([0-9a-fA-F\-]{36})')
if ($mvidMatch.Success) {
    $runtimeMvid = $mvidMatch.Groups[1].Value.ToLowerInvariant()
}

$normalizedExpectedMvid = $expectedModuleVersionId.ToLowerInvariant()
$mvidMatches = $runtimeMvid -eq $normalizedExpectedMvid

Write-Host 'LongLive host runtime check'
Write-Host ('Deploy check : ' + ($(if ($deployCheckSucceeded) { 'passed' } else { 'failed' })))
Write-Host ('Log check    : ' + ($(if ($logReadSucceeded) { 'passed' } else { 'failed' })))
Write-Host ('Log path     : ' + $resolvedLogPath)
Write-Host ('Startup block: ' + $startupBlockLineCount + ' lines')
Write-Host ('Game running : ' + ($(if ($runningGameProcesses.Count -gt 0) { 'true' } else { 'false' })))
Write-Host ('Feature line : ' + $hasFeatureState)
Write-Host ('MVID line    : ' + $hasMvidLine)
Write-Host ('Expected MVID: ' + $normalizedExpectedMvid)
Write-Host ('Runtime MVID : ' + $(if ($runtimeMvid) { $runtimeMvid } else { '<missing>' }))
Write-Host ('MVID match   : ' + $mvidMatches)
Write-Host ('Handshake    : ' + $hasHandshake)
Write-Host ('Bootstrap    : ' + $hasBootstrapCompleted)
Write-Host ('Plugin awake : ' + $hasPluginAwake)

Write-Host ''
Write-Host 'Deploy check output:'
Write-Host $deployCheckOutput.TrimEnd()

Write-Host ''
Write-Host 'Runtime log excerpt:'
Write-Host $logReadOutput.TrimEnd()

if (-not $deployCheckSucceeded) {
    throw 'LongLive host runtime check failed because deploy verification did not pass.'
}

if (-not $logReadSucceeded) {
    throw 'LongLive host runtime check failed because the BepInEx log could not be read.'
}

$staleStartupBlock =
    -not $hasFeatureState -and
    -not $hasMvidLine -and
    $deployCheckSucceeded -and
    ($hasPluginAwake -or $hasHandshake -or $hasBootstrapCompleted) -and
    $runningGameProcesses.Count -eq 0

if ($staleStartupBlock) {
    throw 'LongLive host runtime check found an older startup block in LogOutput.log. The deployed host files already match the current build, but the game is not running and the log does not yet contain a startup from the current deploy. Launch the game once with the current host build, exit, then rerun this check.'
}

if (-not $hasFeatureState) {
    throw 'LongLive host runtime check failed because the current log tail does not contain the new LongLive feature-state startup summary.'
}

if (-not $hasMvidLine) {
    throw 'LongLive host runtime check failed because the current log tail does not contain the LongLive host module MVID line.'
}

if (-not $mvidMatches) {
    throw 'LongLive host runtime check failed because the runtime MVID does not match the current local build output.'
}

Write-Host ''
Write-Host 'LongLive host runtime check passed. The deploy state matches the current build output and the runtime log contains the expected current-build startup identity markers.'
