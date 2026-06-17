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

$requiredPaths = @(
    (Join-Path $bepInExCoreDir "BepInEx.Core.dll"),
    (Join-Path $bepInExCoreDir "BepInEx.Unity.Mono.dll"),
    (Join-Path $gameRoot "觅长生_Data\Managed\UnityEngine.dll"),
    (Join-Path $gameRoot "觅长生_Data\Managed\UnityEngine.CoreModule.dll")
)

foreach ($path in $requiredPaths) {
    if (-not (Test-Path $path)) {
        throw "Required host reference not found: $path"
    }
}

Push-Location $repoRoot
try {
    dotnet build .\src\LongLive.BepInEx\LongLive.BepInEx.csproj -c $Configuration
}
finally {
    Pop-Location
}
