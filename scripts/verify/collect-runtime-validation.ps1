param(
    [int]$Tail = 1200,
    [string]$LogPath = '',
    [ValidateSet('Auto', 'Tail', 'LatestStartup')]
    [string]$Scope = 'Auto',
    [string[]]$Mode = @('All')
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

function Get-ConfiguredGameProcesses {
    param(
        [string]$ResolvedGameRoot
    )

    if ([string]::IsNullOrWhiteSpace($ResolvedGameRoot)) {
        return @()
    }

    return @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.Path -like (Join-Path $ResolvedGameRoot '*')
    })
}

function Resolve-HostLogPath {
    param(
        [string]$ConfiguredLogPath,
        [string]$ResolvedGameRoot,
        [string]$ResolvedBepInExCoreDir
    )

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredLogPath)) {
        if (-not (Test-Path $ConfiguredLogPath)) {
            throw "Configured log path does not exist: $ConfiguredLogPath"
        }

        return $ConfiguredLogPath
    }

    $coreParent = if ([string]::IsNullOrWhiteSpace($ResolvedBepInExCoreDir)) { '' } else { Split-Path -Parent $ResolvedBepInExCoreDir }
    $candidatePaths = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($coreParent)) {
        $candidatePaths.Add((Join-Path $coreParent 'LogOutput.log'))
    }

    if (-not [string]::IsNullOrWhiteSpace($ResolvedGameRoot)) {
        $candidatePaths.Add((Join-Path $ResolvedGameRoot 'BepInEx\LogOutput.log'))
    }

    $resolvedPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($resolvedPath)) {
        throw 'BepInEx LogOutput.log not found. Pass -LogPath explicitly if the runtime layout is unusual.'
    }

    return $resolvedPath
}

function Write-Section {
    param(
        [string]$Title,
        [string[]]$Lines,
        [int]$MaxLines = 40
    )

    Write-Host ''
    Write-Host ('=== ' + $Title + ' ===')

    if (-not $Lines -or $Lines.Count -eq 0) {
        Write-Host '<no matching lines>'
        return
    }

    $displayLines = if ($Lines.Count -le $MaxLines) {
        $Lines
    }
    else {
        $Lines[($Lines.Count - $MaxLines)..($Lines.Count - 1)]
    }

    if ($displayLines.Count -lt $Lines.Count) {
        Write-Host ("<showing last {0} of {1} matching lines>" -f $displayLines.Count, $Lines.Count)
    }

    $displayLines | ForEach-Object { Write-Host $_ }
}

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

function Resolve-SectionSelection {
    param(
        [string[]]$RequestedMode
    )

    $allowedModes = @('All', 'Startup', 'Bulk', 'PopTips', 'Pinyin', 'Fade', 'Battle')
    $normalizedModes = New-Object System.Collections.Generic.List[string]

    foreach ($entry in $RequestedMode) {
        if ([string]::IsNullOrWhiteSpace($entry)) {
            continue
        }

        foreach ($part in ($entry -split ',')) {
            $trimmed = $part.Trim()
            if ([string]::IsNullOrWhiteSpace($trimmed)) {
                continue
            }

            if ($allowedModes -notcontains $trimmed) {
                throw "Unknown mode '$trimmed'. Allowed values: $($allowedModes -join ', ')"
            }

            if ($normalizedModes -notcontains $trimmed) {
                $normalizedModes.Add($trimmed)
            }
        }
    }

    if ($normalizedModes.Count -eq 0 -or $normalizedModes -contains 'All') {
        return @('Startup', 'Bulk', 'PopTips', 'Pinyin', 'Fade', 'Battle')
    }

    return $normalizedModes.ToArray()
}

$resolvedLogPath = Resolve-HostLogPath -ConfiguredLogPath $LogPath -ResolvedGameRoot $gameRoot -ResolvedBepInExCoreDir $bepInExCoreDir
$sectionSelection = Resolve-SectionSelection -RequestedMode $Mode
$runningGameProcesses = Get-ConfiguredGameProcesses -ResolvedGameRoot $gameRoot

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

$sections = @{
    Startup = [pscustomobject]@{
        Title = 'Startup Identity'
        Pattern = 'LongLive feature state:|LongLive host module MVID:|LongLive handshake ready|LongLive host bootstrap completed\.|LongLive .*plugin awake\.'
        MaxLines = 20
    }
    Bulk = [pscustomobject]@{
        Title = 'Bulk Item Use'
        Pattern = '\[BulkItemUse\]'
        MaxLines = 40
    }
    PopTips = [pscustomobject]@{
        Title = 'Pop Tip Optimization'
        Pattern = '\[PopTipOptimization\]'
        MaxLines = 40
    }
    Pinyin = [pscustomobject]@{
        Title = 'Pinyin Search'
        Pattern = '\[PinyinSearch\]'
        MaxLines = 40
    }
    Fade = [pscustomobject]@{
        Title = 'Fade Optimization'
        Pattern = '\[FadeOptimization\]'
        MaxLines = 40
    }
    Battle = [pscustomobject]@{
        Title = 'Battle Trace'
        Pattern = '\[BattleTrace\]'
        MaxLines = 60
    }
}

Write-Host 'LongLive runtime validation log collection'
Write-Host ('Log path : ' + $resolvedLogPath)
Write-Host ('Scope    : ' + $effectiveScope)
Write-Host ('Tail size: ' + $Tail)
Write-Host ('Mode     : ' + ($sectionSelection -join ', '))
Write-Host ('Game run : ' + ($(if ($runningGameProcesses.Count -gt 0) { 'true' } else { 'false' })))

if ($effectiveScope -eq 'LatestStartup') {
    Write-Host ('Startup  : ' + $(if ($startupBlockFound) { "found at line $startupBlockStartIndex" } else { 'not found, tail fallback used' }))
}

$startupIdentityLines = $lineSource | Where-Object {
    $_ -match 'LongLive feature state:' -or
    $_ -match 'LongLive host module MVID:' -or
    $_ -match 'LongLive handshake ready' -or
    $_ -match 'LongLive host bootstrap completed\.' -or
    $_ -match 'LongLive .*plugin awake\.'
}

$hasFeatureState = $startupIdentityLines | Where-Object { $_ -match 'LongLive feature state:' } | Select-Object -First 1
$hasMvidLine = $startupIdentityLines | Where-Object { $_ -match 'LongLive host module MVID:' } | Select-Object -First 1
$hasOldStartupOnly =
    -not $hasFeatureState -and
    -not $hasMvidLine -and
    ($startupIdentityLines | Where-Object { $_ -match 'LongLive .*plugin awake\.|LongLive host bootstrap completed\.|LongLive handshake ready' } | Select-Object -First 1)

if ($hasOldStartupOnly -and $runningGameProcesses.Count -eq 0) {
    Write-Host ''
    Write-Host 'Startup state: stale-startup-block-detected'
    Write-Host 'The current log scope still points at an older LongLive startup block. Deploy state may already be current, but this log evidence does not yet prove that the current deploy was launched.'
}

foreach ($sectionKey in $sectionSelection) {
    $section = $sections[$sectionKey]
    if ($null -eq $section) {
        throw "Unknown section selection: $sectionKey"
    }

    $matchedLines = $lineSource | Where-Object { $_ -match $section.Pattern }
    Write-Section -Title $section.Title -Lines $matchedLines -MaxLines $section.MaxLines
}
