param(
    [string]$Configuration = "Debug",
    [string]$PluginsDir = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
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

Push-Location $repoRoot
try {
    & (Join-Path $PSScriptRoot "build-host.ps1") -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Host build failed with exit code $LASTEXITCODE. Deployment aborted."
    }

    $outputDir = Join-Path $repoRoot "src\LongLive.BepInEx\bin\$Configuration\net472"
    $dllPath = Join-Path $outputDir "LongLive.BepInEx.dll"

    if (-not (Test-Path $dllPath)) {
        throw "Built plugin DLL not found: $dllPath"
    }

    $copyPatterns = @(
        'LongLive.BepInEx.*',
        'LongLive.Mods.*',
        'LongLive.Next.Runtime.*',
        'LongLive.Next.Abstractions.*',
        'Microsoft.Bcl.AsyncInterfaces.dll',
        'System.Buffers.dll',
        'System.Memory.dll',
        'System.Numerics.Vectors.dll',
        'System.Runtime.CompilerServices.Unsafe.dll',
        'System.Text.Encodings.Web.dll',
        'System.Text.Json.dll',
        'System.Threading.Tasks.Extensions.dll',
        'System.ValueTuple.dll'
    )

    foreach ($pattern in $copyPatterns) {
        $files = Get-ChildItem -Path $outputDir -Filter $pattern -File -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $resolvedPluginsDir $file.Name) -Force
        }
    }

    $assetSourceDir = Join-Path $repoRoot 'src\LongLive.BepInEx\LongLiveAssets'
    if (Test-Path $assetSourceDir) {
        $assetTargetDir = Join-Path $resolvedPluginsDir 'LongLiveAssets'
        New-Item -ItemType Directory -Force -Path $assetTargetDir | Out-Null
        Copy-Item -Path (Join-Path $assetSourceDir '*') -Destination $assetTargetDir -Recurse -Force
    }

    Write-Host "Deployed LongLive.BepInEx to: $resolvedPluginsDir"
}
finally {
    Pop-Location
}
