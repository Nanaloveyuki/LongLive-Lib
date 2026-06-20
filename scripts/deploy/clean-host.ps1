param(
    [string]$PluginsDir = "",
    [switch]$RemoveExports
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$propsPath = Join-Path $repoRoot "eng\LocalReferences.props"

if (-not (Test-Path $propsPath)) {
    throw "Missing eng/LocalReferences.props. Copy eng/LocalReferences.props.example first."
}

[xml]$propsXml = [System.IO.File]::ReadAllText($propsPath, [System.Text.Encoding]::UTF8)
$propertyGroup = $propsXml.Project.PropertyGroup

$gameRoot = [string]$propertyGroup.McsGameRoot
$bepInExCoreDir = [string]$propertyGroup.BepInExCoreDir

if ([string]::IsNullOrWhiteSpace($gameRoot)) {
    throw "McsGameRoot is empty in eng/LocalReferences.props."
}

if ([string]::IsNullOrWhiteSpace($bepInExCoreDir)) {
    throw "BepInExCoreDir is empty in eng/LocalReferences.props."
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

$filesToRemove = @(
    'LongLive.BepInEx.dll',
    'LongLive.BepInEx.pdb',
    'LongLive.BepInEx.xml',
    'LongLive.Mods.dll',
    'LongLive.Mods.pdb',
    'LongLive.Mods.xml',
    'LongLive.Next.Abstractions.dll',
    'LongLive.Next.Abstractions.pdb',
    'LongLive.Next.Abstractions.xml',
    'LongLive.Next.Runtime.dll',
    'LongLive.Next.Runtime.pdb',
    'LongLive.Next.Runtime.xml',
    'longlive_native_core.dll'
)

$directoriesToRemove = @(
    'LongLiveAssets'
)

if ($RemoveExports) {
    $directoriesToRemove += 'LongLiveExports'
}

foreach ($name in $filesToRemove) {
    $path = Join-Path $resolvedPluginsDir $name
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
        Write-Host "Removed host file: $path"
    }
}

foreach ($name in $directoriesToRemove) {
    $path = Join-Path $resolvedPluginsDir $name
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
        Write-Host "Removed host directory: $path"
    }
}

Write-Host "LongLive host cleanup completed for: $resolvedPluginsDir"
