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
    $outputDir = Join-Path $repoRoot "src\LongLive.BepInEx\bin\$Configuration\net472"
    $dllPath = Join-Path $outputDir "LongLive.BepInEx.dll"

    $buildScript = Join-Path $PSScriptRoot "build-host.ps1"
    $buildFailed = $false
    $buildErrorMessage = $null

    try {
        & $buildScript -Configuration $Configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Host build failed with exit code $LASTEXITCODE. Deployment aborted."
        }
    }
    catch {
        $buildFailed = $true
        $buildErrorMessage = $_.Exception.Message
    }

    if ($buildFailed -and -not (Test-Path $dllPath)) {
        throw $buildErrorMessage
    }

    if ($buildFailed) {
        Write-Warning "Host build reported a failure, but an existing output DLL was found. Continuing deployment with the existing build output."
        Write-Warning $buildErrorMessage
    }

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
            try {
                Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $resolvedPluginsDir $file.Name) -Force
            }
            catch [System.IO.IOException] {
                Write-Warning "Skipped copying locked file: $($file.Name). Close the game or any process that currently loads it, then rerun deploy-host.ps1 to replace it."
            }
        }
    }

    $assetSourceDir = Join-Path $repoRoot 'src\LongLive.BepInEx\LongLiveAssets'
    if (Test-Path $assetSourceDir) {
        $assetTargetDir = Join-Path $resolvedPluginsDir 'LongLiveAssets'
        New-Item -ItemType Directory -Force -Path $assetTargetDir | Out-Null
        try {
            Copy-Item -Path (Join-Path $assetSourceDir '*') -Destination $assetTargetDir -Recurse -Force
        }
        catch [System.IO.IOException] {
            Write-Warning "Skipped copying one or more LongLive asset files because a target file is locked."
        }
    }

    $nativeLibraryPath = Join-Path $repoRoot 'native\target\debug\longlive_native_core.dll'
    if (Test-Path $nativeLibraryPath) {
        try {
            Copy-Item -LiteralPath $nativeLibraryPath -Destination (Join-Path $resolvedPluginsDir 'longlive_native_core.dll') -Force
        }
        catch [System.IO.IOException] {
            Write-Warning "Skipped copying locked native library file: longlive_native_core.dll"
        }
    }

    Write-Host "Deployed LongLive.BepInEx to: $resolvedPluginsDir"
}
finally {
    Pop-Location
}
