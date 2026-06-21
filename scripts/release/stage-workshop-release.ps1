param(
    [string]$Configuration = "Debug",
    [string]$Version = "0.2.1",
    [string]$OutputRoot = "artifacts\workshop",
    [string]$PackageName = "LongLive.Lib",
    [string]$BridgePackage = "docs\samples\next-bridge-demo",
    [string]$NextModName = "modLongLiveBridge",
    [string]$DisplayName = "LongLive Lib",
    [string]$Description = "LongLive Lib \u4e3b\u7a0b\u5e8f\u4e0e\u517c\u5bb9\u6865\u63a5\u3002LongLive Host and compatibility bridge.",
    [switch]$SkipBridge,
    [switch]$SkipAssets,
    [switch]$IncludeNative,
    [switch]$DisableMissingHostReminder
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$buildScript = Join-Path (Join-Path (Split-Path -Parent $PSScriptRoot) "deploy") "build-host.ps1"
$bridgePackagePath = Join-Path $repoRoot $BridgePackage
$outputDir = Join-Path $repoRoot "src\LongLive.BepInEx\bin\$Configuration\net472"
$stageRoot = Join-Path $repoRoot (Join-Path $OutputRoot $PackageName)
$pluginsRoot = Join-Path $stageRoot "plugins"
$preservedModBinBytes = $null

function Get-LegacyModBinPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkshopRoot,
        [Parameter(Mandatory = $true)]
        [string]$PackageName,
        [Parameter(Mandatory = $true)]
        [string]$CurrentStageRoot
    )

    if (-not (Test-Path $WorkshopRoot)) {
        return $null
    }

    $legacyModBinPath = Get-ChildItem -LiteralPath $WorkshopRoot -Directory -Force |
        Where-Object {
            $_.FullName -ne $CurrentStageRoot -and $_.Name -like ($PackageName + ".*")
        } |
        Sort-Object LastWriteTime -Descending |
        ForEach-Object {
            $candidatePath = Join-Path $_.FullName "Mod.bin"
            if (Test-Path $candidatePath) {
                return $candidatePath
            }
        } |
        Select-Object -First 1

    return $legacyModBinPath
}

function Copy-FilteredTree {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceRoot,
        [Parameter(Mandatory = $true)]
        [string]$DestinationRoot
    )

    if (-not (Test-Path $SourceRoot)) {
        return
    }

    New-Item -ItemType Directory -Force -Path $DestinationRoot | Out-Null

    Get-ChildItem -LiteralPath $SourceRoot -Recurse -Force | ForEach-Object {
        $relativePath = $_.FullName.Substring($SourceRoot.Length).TrimStart([char]92, [char]47)
        if ([string]::IsNullOrWhiteSpace($relativePath)) {
            return
        }

        if ($_.PSIsContainer) {
            $targetDir = Join-Path $DestinationRoot $relativePath
            New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
            return
        }

        if ($_.Name -in @('.gitkeep', '.DS_Store', 'Thumbs.db')) {
            return
        }

        $targetPath = Join-Path $DestinationRoot $relativePath
        $targetDirectory = Split-Path -Parent $targetPath
        New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $targetPath -Force
    }
}

if (-not (Test-Path $buildScript)) {
    throw "Build script not found: $buildScript"
}

if (-not $SkipBridge -and -not (Test-Path $bridgePackagePath)) {
    throw "Bridge package not found: $bridgePackagePath"
}

Push-Location $repoRoot
try {
    & $buildScript -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Host build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

$resolvedStageRoot = [System.IO.Path]::GetFullPath($stageRoot)
$resolvedRepoRoot = [System.IO.Path]::GetFullPath($repoRoot)
if (-not $resolvedStageRoot.StartsWith($resolvedRepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Resolved stage root escaped repository root: $resolvedStageRoot"
}

if (Test-Path $resolvedStageRoot) {
    $modBinPath = Join-Path $resolvedStageRoot "Mod.bin"
    if (Test-Path $modBinPath) {
        $preservedModBinBytes = [System.IO.File]::ReadAllBytes($modBinPath)
    }

    Get-ChildItem -LiteralPath $resolvedStageRoot -Force | Where-Object { $_.Name -ne 'Mod.bin' } | Remove-Item -Recurse -Force
}

if ($preservedModBinBytes -eq $null) {
    $legacyModBinPath = Get-LegacyModBinPath -WorkshopRoot (Join-Path $repoRoot $OutputRoot) -PackageName $PackageName -CurrentStageRoot $resolvedStageRoot
    if ($null -ne $legacyModBinPath) {
        $preservedModBinBytes = [System.IO.File]::ReadAllBytes($legacyModBinPath)
        Write-Host "Recovered Mod.bin from legacy staged package: $legacyModBinPath"
    }
}

New-Item -ItemType Directory -Force -Path $pluginsRoot | Out-Null

if ($preservedModBinBytes -ne $null) {
    [System.IO.File]::WriteAllBytes((Join-Path $resolvedStageRoot "Mod.bin"), $preservedModBinBytes)
}

$copyFiles = @(
    "LongLive.BepInEx.dll",
    "LongLive.Mods.dll",
    "LongLive.Next.Abstractions.dll",
    "LongLive.Next.Runtime.dll",
    "Microsoft.Bcl.AsyncInterfaces.dll",
    "System.Buffers.dll",
    "System.Memory.dll",
    "System.Numerics.Vectors.dll",
    "System.Runtime.CompilerServices.Unsafe.dll",
    "System.Text.Encodings.Web.dll",
    "System.Text.Json.dll",
    "System.Threading.Tasks.Extensions.dll",
    "System.ValueTuple.dll"
)

foreach ($fileName in $copyFiles) {
    $sourcePath = Join-Path $outputDir $fileName
    if (-not (Test-Path $sourcePath)) {
        throw "Required release file not found: $sourcePath"
    }

    Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $pluginsRoot $fileName) -Force
}

if (-not $SkipAssets) {
    $assetSourceDir = Join-Path $repoRoot "src\LongLive.BepInEx\LongLiveAssets"
    if (Test-Path $assetSourceDir) {
        $assetTargetDir = Join-Path $pluginsRoot "LongLiveAssets"
        Copy-FilteredTree -SourceRoot $assetSourceDir -DestinationRoot $assetTargetDir
    }
}

if ($IncludeNative) {
    $nativeLibraryPath = Join-Path $repoRoot "native\target\debug\longlive_native_core.dll"
    if (-not (Test-Path $nativeLibraryPath)) {
        throw "Native library requested but not found: $nativeLibraryPath"
    }

    Copy-Item -LiteralPath $nativeLibraryPath -Destination (Join-Path $pluginsRoot "longlive_native_core.dll") -Force
}

if (-not $SkipBridge) {
    if ($NextModName -notlike "mod*") {
        throw "NextModName must start with 'mod'."
    }

    $targetModRoot = Join-Path $pluginsRoot "Next\$NextModName"
    $configDir = Join-Path $targetModRoot "Config"
    $modConfigPath = Join-Path $configDir "modConfig.json"
    $enableMissingHostReminder = -not $DisableMissingHostReminder
    $missingHostReminderJson = if ($enableMissingHostReminder) { "true" } else { "false" }

    New-Item -ItemType Directory -Force -Path $targetModRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $configDir | Out-Null
    Copy-FilteredTree -SourceRoot $bridgePackagePath -DestinationRoot $targetModRoot

    $modConfig = @'
{
  "Name": "__DISPLAY_NAME__",
  "Author": "nanaloveyuki",
  "Version": "__VERSION__",
  "Description": "__DESCRIPTION__",
  "Settings": [
    {
      "Type": "Toggle",
      "Key": "longlive.bridge.enable_missing_host_reminder",
      "Name": "Host \u5b89\u88c5\u63d0\u793a",
      "Description": "\u7f3a\u5c11\u6216\u4e0d\u517c\u5bb9 LongLive Host \u65f6\u663e\u793a\u63d0\u793a\u3002Show a reminder if LongLive Host is missing or incompatible.",
      "DefaultValue": __ENABLE_MISSING_HOST_REMINDER__
    }
  ]
}
'@

    $modConfig = $modConfig.Replace("__DISPLAY_NAME__", $DisplayName)
    $modConfig = $modConfig.Replace("__VERSION__", $Version)
    $modConfig = $modConfig.Replace("__DESCRIPTION__", $Description)
    $modConfig = $modConfig.Replace("__ENABLE_MISSING_HOST_REMINDER__", $missingHostReminderJson)

    Set-Content -LiteralPath $modConfigPath -Value $modConfig -Encoding UTF8
}

Write-Host "Staged workshop package root: $resolvedStageRoot"
Write-Host "Upload this folder in the in-game workshop uploader (the folder that directly contains plugins)."
Write-Host "Requested version label: $Version"
Write-Host "Bridge included: $(-not $SkipBridge)"
Write-Host "Native included: $IncludeNative"
Write-Host "Assets included: $(-not $SkipAssets)"
Write-Host "Mod.bin preserved: $($null -ne $preservedModBinBytes)"
