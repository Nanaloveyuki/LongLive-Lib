param(
    [string]$Configuration = "Debug",
    [string]$PluginsDir = ""
)

$ErrorActionPreference = "Stop"

function Get-LongLiveFileState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [string]$SourceDir,

        [Parameter(Mandatory = $true)]
        [string]$TargetDir
    )

    $sourcePath = Join-Path $SourceDir $Name
    $targetPath = Join-Path $TargetDir $Name

    $sourceExists = Test-Path $sourcePath
    $targetExists = Test-Path $targetPath

    $sourceLength = $null
    $sourceWriteTime = $null
    $targetLength = $null
    $targetWriteTime = $null
    $hashMatches = $false
    $status = "missing-source"

    if ($sourceExists) {
        $sourceItem = Get-Item -LiteralPath $sourcePath
        $sourceLength = $sourceItem.Length
        $sourceWriteTime = $sourceItem.LastWriteTime
        $status = "missing-target"
    }

    if ($targetExists) {
        $targetItem = Get-Item -LiteralPath $targetPath
        $targetLength = $targetItem.Length
        $targetWriteTime = $targetItem.LastWriteTime
    }

    if ($sourceExists -and $targetExists) {
        if ($sourceLength -eq $targetLength) {
            $sourceHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
            $targetHash = (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash
            $hashMatches = $sourceHash -eq $targetHash
            $status = if ($hashMatches) { "match" } else { "mismatch" }
        }
        else {
            $status = "mismatch"
        }
    }

    [pscustomobject]@{
        Name = $Name
        Status = $status
        SourceLength = $sourceLength
        SourceLastWriteTime = $sourceWriteTime
        TargetLength = $targetLength
        TargetLastWriteTime = $targetWriteTime
        HashMatches = $hashMatches
    }
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$propsPath = Join-Path $repoRoot "eng\LocalReferences.props"

if (-not (Test-Path $propsPath)) {
    throw "Missing eng/LocalReferences.props. Copy eng/LocalReferences.props.example first."
}

[xml]$propsXml = [System.IO.File]::ReadAllText($propsPath, [System.Text.Encoding]::UTF8)
$propertyGroup = $propsXml.Project.PropertyGroup

$enabled = [string]$propertyGroup.LongLiveEnableLocalHostReferences
$gameRoot = [string]$propertyGroup.McsGameRoot
$bepInExCoreDir = [string]$propertyGroup.BepInExCoreDir

if ($enabled -ne "true") {
    throw "LongLiveEnableLocalHostReferences must be set to true in eng/LocalReferences.props."
}

$resolvedPluginsDir = $PluginsDir
if ([string]::IsNullOrWhiteSpace($resolvedPluginsDir)) {
    $coreDir = Split-Path -Parent $bepInExCoreDir
    $candidateDirs = @(
        (Join-Path $gameRoot "BepInEx\plugins"),
        (Join-Path $coreDir "plugins")
    )

    $resolvedPluginsDir = $candidateDirs | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($resolvedPluginsDir)) {
    throw "BepInEx plugins directory not found. Checked game-root and core-adjacent plugin directories."
}

$outputDir = Join-Path $repoRoot "src\LongLive.BepInEx\bin\$Configuration\net472"
if (-not (Test-Path $outputDir)) {
    throw "Build output directory not found: $outputDir"
}

$runningGameProcesses = Get-Process -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -like (Join-Path $gameRoot '*')
} | Select-Object ProcessName, Id, Path, StartTime

$requiredFiles = @(
    'LongLive.BepInEx.dll',
    'LongLive.Mods.dll',
    'LongLive.Next.Runtime.dll',
    'LongLive.Next.Abstractions.dll'
)

$fileStates = foreach ($name in $requiredFiles) {
    Get-LongLiveFileState -Name $name -SourceDir $outputDir -TargetDir $resolvedPluginsDir
}

$hasMismatch = $fileStates | Where-Object { $_.Status -ne 'match' }

Write-Host "LongLive host deploy check"
Write-Host "Source output : $outputDir"
Write-Host "Target plugins: $resolvedPluginsDir"

if ($runningGameProcesses) {
    Write-Warning "Game-root processes are still running:"
    $runningGameProcesses | Format-Table -AutoSize | Out-String | Write-Host
}
else {
    Write-Host "No running processes detected under the configured game root."
}

$fileStates | Format-Table Name, Status, SourceLength, TargetLength, SourceLastWriteTime, TargetLastWriteTime -AutoSize | Out-String | Write-Host

if ($hasMismatch) {
    throw "LongLive host deploy check failed. One or more required host files do not match the current build output."
}

Write-Host "LongLive host deploy check passed. Required host files match the current build output."
