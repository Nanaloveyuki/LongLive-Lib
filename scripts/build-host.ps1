param(
    [string]$Configuration = "Debug"
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

$managedDir = Get-ChildItem -Path $gameRoot -Directory -Filter '*_Data' -ErrorAction SilentlyContinue |
    ForEach-Object { Join-Path $_.FullName 'Managed' } |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($managedDir)) {
    throw "Unity managed directory not found under game root: $gameRoot"
}

$requiredPaths = @(
    (Join-Path $bepInExCoreDir "BepInEx.dll"),
    (Join-Path $managedDir "UnityEngine.dll"),
    (Join-Path $managedDir "UnityEngine.CoreModule.dll")
)

foreach ($path in $requiredPaths) {
    if (-not (Test-Path $path)) {
        throw "Required host reference not found: $path"
    }
}

Push-Location $repoRoot
try {
    dotnet build-server shutdown | Out-Null
    dotnet build .\src\LongLive.BepInEx\LongLive.BepInEx.csproj -c $Configuration --disable-build-servers -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) {
        throw "Host build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
