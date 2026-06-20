param(
    [string]$Pattern = 'LongLive',
    [int]$Tail = 200,
    [ValidateSet('Tail', 'LatestStartup', 'Auto')]
    [string]$Scope = 'Tail',
    [string]$LogPath = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$propsPath = Join-Path $repoRoot 'eng\LocalReferences.props'

if (-not (Test-Path $propsPath)) {
    throw 'Missing eng/LocalReferences.props. Copy eng/LocalReferences.props.example first.'
}

[xml]$propsXml = [System.IO.File]::ReadAllText($propsPath, [System.Text.Encoding]::UTF8)
$propertyGroup = $propsXml.Project.PropertyGroup
$gameRoot = [string]$propertyGroup.McsGameRoot
$bepInExCoreDir = [string]$propertyGroup.BepInExCoreDir

function Get-LatestLongLiveStartupBlock {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedLogPath,

        [Parameter(Mandatory = $true)]
        [int]$FallbackTail
    )

    $allLines = Get-Content -LiteralPath $ResolvedLogPath
    $awakePattern = 'LongLive .*plugin awake\.'
    $awakeIndexes = for ($index = 0; $index -lt $allLines.Count; $index++) {
        if ($allLines[$index] -match $awakePattern) {
            $index
        }
    }

    if ($awakeIndexes.Count -gt 0) {
        $startIndex = $awakeIndexes[$awakeIndexes.Count - 1]
        return [pscustomobject]@{
            Found = $true
            StartIndex = $startIndex
            Lines = $allLines[$startIndex..($allLines.Count - 1)]
        }
    }

    return [pscustomobject]@{
        Found = $false
        StartIndex = -1
        Lines = Get-Content -LiteralPath $ResolvedLogPath -Tail $FallbackTail
    }
}

$resolvedLogPath = $LogPath
if ([string]::IsNullOrWhiteSpace($resolvedLogPath)) {
    $coreParent = if ([string]::IsNullOrWhiteSpace($bepInExCoreDir)) { '' } else { Split-Path -Parent $bepInExCoreDir }

    $candidatePaths = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($coreParent)) {
        $candidatePaths.Add((Join-Path $coreParent 'LogOutput.log'))
    }

    if (-not [string]::IsNullOrWhiteSpace($gameRoot)) {
        $candidatePaths.Add((Join-Path $gameRoot 'BepInEx\LogOutput.log'))
    }

    $resolvedLogPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($resolvedLogPath) -or -not (Test-Path $resolvedLogPath)) {
    throw 'BepInEx LogOutput.log not found. Pass -LogPath explicitly if the runtime layout is unusual.'
}

$lineSource = $null
$effectiveScope = $Scope
$startupBlockFound = $false
$startupBlockStartIndex = -1

switch ($Scope) {
    'Tail' {
        $lineSource = Get-Content -LiteralPath $resolvedLogPath -Tail $Tail
    }
    'LatestStartup' {
        $startupBlock = Get-LatestLongLiveStartupBlock -ResolvedLogPath $resolvedLogPath -FallbackTail $Tail
        $lineSource = $startupBlock.Lines
        $startupBlockFound = $startupBlock.Found
        $startupBlockStartIndex = $startupBlock.StartIndex
    }
    default {
        $startupBlock = Get-LatestLongLiveStartupBlock -ResolvedLogPath $resolvedLogPath -FallbackTail $Tail
        $lineSource = $startupBlock.Lines
        $startupBlockFound = $startupBlock.Found
        $startupBlockStartIndex = $startupBlock.StartIndex
        $effectiveScope = if ($startupBlockFound) { 'LatestStartup' } else { 'Tail' }
    }
}

$matchedLines = if ([string]::IsNullOrWhiteSpace($Pattern)) {
    $lineSource
}
else {
    $lineSource | Where-Object { $_ -match $Pattern }
}

Write-Host "LongLive host log read"
Write-Host "Log path : $resolvedLogPath"
Write-Host "Tail size: $Tail"
Write-Host "Scope    : $effectiveScope"
Write-Host "Pattern  : $Pattern"

if ($effectiveScope -eq 'LatestStartup') {
    Write-Host ('Startup  : ' + $(if ($startupBlockFound) { "found at line $startupBlockStartIndex" } else { 'not found, tail fallback used' }))
}

if (-not $matchedLines -or $matchedLines.Count -eq 0) {
    Write-Warning 'No matching lines were found in the requested tail window.'
    exit 1
}

$matchedLines | ForEach-Object { Write-Host $_ }
