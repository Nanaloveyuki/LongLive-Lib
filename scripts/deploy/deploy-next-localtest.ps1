param(
    [string]$BridgePackage = "docs\samples\next-bridge-demo",
    [string]$SourcePackage = "docs\samples\json-mod-demo",
    [string]$GameRoot = "D:\Appdata\Steam\steamapps\common\觅长生",
    [string]$LocalTestGroupName = "LongLive.LocalTest",
    [string]$NextModName = "modLongLiveBridge",
    [string]$DisplayName = "LongLive Lib",
    [string]$Description = "LongLive Lib \u4e3b\u7a0b\u5e8f\u4e0e\u517c\u5bb9\u6865\u63a5\u3002LongLive Host and compatibility bridge.",
    [switch]$DisableMissingHostReminder,
    [switch]$SkipBridgePackage,
    [switch]$SkipJsonDemo
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$bridgePackagePath = Join-Path $repoRoot $BridgePackage
$sourcePath = Join-Path $repoRoot $SourcePackage

if (-not $SkipBridgePackage -and -not (Test-Path $bridgePackagePath)) {
    throw "Bridge package not found: $bridgePackagePath"
}

if (-not $SkipJsonDemo -and -not (Test-Path $sourcePath)) {
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

if ($NextModName -notlike 'mod*') {
    throw "NextModName must start with 'mod' to match Next local mod discovery expectations."
}

$enableMissingHostReminder = -not $DisableMissingHostReminder

New-Item -ItemType Directory -Force -Path $targetModRoot | Out-Null
New-Item -ItemType Directory -Force -Path $configDir | Out-Null

if (-not $SkipBridgePackage) {
    Copy-Item -Path (Join-Path $bridgePackagePath '*') -Destination $targetModRoot -Recurse -Force
}

if (-not $SkipJsonDemo) {
    New-Item -ItemType Directory -Force -Path $targetPackageRoot | Out-Null
    Copy-Item -Path (Join-Path $sourcePath '*') -Destination $targetPackageRoot -Recurse -Force
}

$missingHostReminderJson = if ($enableMissingHostReminder) { 'true' } else { 'false' }

$modConfig = @'
{
  "Name": "__DISPLAY_NAME__",
  "Author": "nanaloveyuki",
  "Version": "0.2.0",
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

$modConfig = $modConfig.Replace('__DISPLAY_NAME__', $DisplayName)
$modConfig = $modConfig.Replace('__DESCRIPTION__', $Description)
$modConfig = $modConfig.Replace('__ENABLE_MISSING_HOST_REMINDER__', $missingHostReminderJson)

Set-Content -LiteralPath $modConfigPath -Value $modConfig -Encoding UTF8

if ($SkipBridgePackage) {
    Write-Host "Skipped staging the Bridge sample package."
}
else {
    Write-Host "Staged LongLive Bridge sample package from: $bridgePackagePath"
}

if ($SkipJsonDemo) {
    Write-Host "Skipped staging the optional JSON demo payload."
}
else {
    Write-Host "Deployed LongLive JSON demo payload to: $targetPackageRoot"
}

Write-Host "LongLive local-test mod root: $targetModRoot"
Write-Host "Missing-host reminder default: $enableMissingHostReminder"
Write-Host "This script stages the content-side shell only. Install LongLive.Host separately into BepInEx/plugins."
