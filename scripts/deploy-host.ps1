param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$propsPath = Join-Path $repoRoot "eng\LocalReferences.props"

if (-not (Test-Path $propsPath)) {
    throw "Missing eng/LocalReferences.props. Copy eng/LocalReferences.props.example first."
}

[xml]$propsXml = Get-Content $propsPath
$propertyGroup = $propsXml.Project.PropertyGroup

$enabled = [string]$propertyGroup.LongLiveEnableLocalHostReferences
$gameRoot = [string]$propertyGroup.McsGameRoot

if ($enabled -ne "true") {
    throw "LongLiveEnableLocalHostReferences must be set to true in eng/LocalReferences.props."
}

if ([string]::IsNullOrWhiteSpace($gameRoot)) {
    throw "McsGameRoot is empty in eng/LocalReferences.props."
}

$pluginsDir = Join-Path $gameRoot "BepInEx\plugins"
if (-not (Test-Path $pluginsDir)) {
    throw "BepInEx plugins directory not found: $pluginsDir"
}

Push-Location $repoRoot
try {
    & (Join-Path $PSScriptRoot "build-host.ps1") -Configuration $Configuration

    $outputDir = Join-Path $repoRoot "src\LongLive.BepInEx\bin\$Configuration\net472"
    $dllPath = Join-Path $outputDir "LongLive.BepInEx.dll"
    $pdbPath = Join-Path $outputDir "LongLive.BepInEx.pdb"
    $depsPath = Join-Path $outputDir "LongLive.BepInEx.deps.json"

    if (-not (Test-Path $dllPath)) {
        throw "Built plugin DLL not found: $dllPath"
    }

    Copy-Item -LiteralPath $dllPath -Destination (Join-Path $pluginsDir "LongLive.BepInEx.dll") -Force

    if (Test-Path $pdbPath) {
        Copy-Item -LiteralPath $pdbPath -Destination (Join-Path $pluginsDir "LongLive.BepInEx.pdb") -Force
    }

    if (Test-Path $depsPath) {
        Copy-Item -LiteralPath $depsPath -Destination (Join-Path $pluginsDir "LongLive.BepInEx.deps.json") -Force
    }
}
finally {
    Pop-Location
}
