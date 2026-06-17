param(
    [string]$SourcePackage = "docs\samples\json-mod-demo",
    [string]$GameRoot = "D:\Appdata\Steam\steamapps\common\觅长生",
    [string]$LocalTestGroupName = "LongLive.LocalTest",
    [string]$NextModName = "modLongLiveDemo"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $repoRoot $SourcePackage

if (-not (Test-Path $sourcePath)) {
    throw "Source JSON mod package not found: $sourcePath"
}

$localModsRoot = Join-Path $GameRoot "本地Mod测试"
if (-not (Test-Path $localModsRoot)) {
    throw "Next local mod root not found: $localModsRoot"
}

$targetRoot = Join-Path $localModsRoot $LocalTestGroupName
$targetModRoot = Join-Path $targetRoot "plugins\Next\$NextModName"
$targetPackageRoot = Join-Path $targetModRoot "LongLive\json-mod-demo"
$configDir = Join-Path $targetModRoot "Config"
$modConfigPath = Join-Path $configDir "modConfig.json"

New-Item -ItemType Directory -Force -Path $targetPackageRoot | Out-Null
New-Item -ItemType Directory -Force -Path $configDir | Out-Null

Copy-Item -Path (Join-Path $sourcePath '*') -Destination $targetPackageRoot -Recurse -Force

$modConfig = @'
{
  "Name": "LongLive Local Test",
  "Author": "nanaloveyuki",
  "Version": "0.1.0",
  "Description": "Local Next patch-mod shell for LongLive JSON demo deployment.",
  "Settings": []
}
'@

Set-Content -LiteralPath $modConfigPath -Value $modConfig -Encoding UTF8

Write-Host "Deployed Next local test package to: $targetPackageRoot"
Write-Host "Next local test mod root: $targetModRoot"
