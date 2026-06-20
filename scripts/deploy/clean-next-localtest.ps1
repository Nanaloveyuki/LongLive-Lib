param(
    [string]$GameRoot = "D:\Appdata\Steam\steamapps\common\觅长生",
    [string]$LocalTestGroupName = "",
    [switch]$AllLongLive
)

$ErrorActionPreference = "Stop"

$localModsRoot = Join-Path $GameRoot "本地Mod测试"
if (-not (Test-Path -LiteralPath $localModsRoot)) {
    throw "Next local mod root not found: $localModsRoot"
}

if ($AllLongLive) {
    $targets = Get-ChildItem -LiteralPath $localModsRoot -Force | Where-Object { $_.PSIsContainer -and $_.Name -like 'LongLive*' }
}
elseif (-not [string]::IsNullOrWhiteSpace($LocalTestGroupName)) {
    $targetPath = Join-Path $localModsRoot $LocalTestGroupName
    $targets = @()
    if (Test-Path -LiteralPath $targetPath) {
        $targets = @(Get-Item -LiteralPath $targetPath)
    }
}
else {
    throw "Specify -LocalTestGroupName <name> or pass -AllLongLive."
}

foreach ($target in $targets) {
    try {
        Remove-Item -LiteralPath $target.FullName -Recurse -Force
        Write-Host "Removed local-test group: $($target.FullName)"
    }
    catch [System.IO.IOException] {
        throw "Failed to remove local-test group because one or more files are locked: $($target.FullName). Close the game, editors, or other tools that may still hold files in this directory, then rerun clean-next-localtest.ps1."
    }
}

Write-Host "LongLive local-test cleanup completed under: $localModsRoot"
