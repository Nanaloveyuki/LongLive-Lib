param(
    [ValidateSet('host-redeploy', 'host-runtime-check', 'localtest-stage', 'release-stage', 'help')]
    [string]$Action = 'help',
    [string]$Configuration = 'Debug',
    [switch]$Wait,
    [switch]$SkipRuntimeCheck,
    [switch]$SkipNative,
    [switch]$IncludeNative,
    [switch]$DisableMissingHostReminder,
    [string]$Version = '0.2.1'
)

$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot

function Invoke-LongLiveScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptName,

        [Parameter()]
        [hashtable]$Arguments = @{}
    )

    $scriptPath = if ([System.IO.Path]::IsPathRooted($ScriptName)) {
        $ScriptName
    }
    else {
        Join-Path $scriptRoot $ScriptName
    }
    if (-not (Test-Path $scriptPath)) {
        throw "Required script not found: $scriptPath"
    }

    & $scriptPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$ScriptName failed with exit code $LASTEXITCODE."
    }
}

function Get-CategoryScriptPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Category,

        [Parameter(Mandatory = $true)]
        [string]$ScriptName
    )

    return Join-Path (Join-Path $scriptRoot $Category) $ScriptName
}

function Show-Help {
    Write-Host 'LongLive unified script entry'
    Write-Host ''
    Write-Host 'Script groups:'
    Write-Host '  scripts/deploy  host deploy and local-test staging'
    Write-Host '  scripts/verify  runtime and log verification'
    Write-Host '  scripts/release workshop upload staging'
    Write-Host '  scripts/test    offline regression checks'
    Write-Host ''
    Write-Host 'Actions:'
    Write-Host '  host-redeploy      Build and deploy LongLive.Host into BepInEx/plugins.'
    Write-Host '  host-runtime-check Verify that the current game log is running the current host build.'
    Write-Host '  localtest-stage    Stage the LongLive local-test content shell under the game local-test mod directory.'
    Write-Host '  release-stage      Stage a workshop upload folder under artifacts/workshop.'
    Write-Host '  help               Show this help.'
    Write-Host ''
    Write-Host 'Examples:'
    Write-Host '  ./scripts/longlive.ps1 -Action host-redeploy'
    Write-Host '  ./scripts/longlive.ps1 -Action host-redeploy -Wait'
    Write-Host '  ./scripts/longlive.ps1 -Action host-runtime-check'
    Write-Host '  ./scripts/longlive.ps1 -Action localtest-stage'
    Write-Host '  ./scripts/longlive.ps1 -Action release-stage -Version 0.2.1'
    Write-Host '  ./scripts/longlive.ps1 -Action release-stage -Version 0.2.1 -IncludeNative'
}

switch ($Action) {
    'host-redeploy' {
        $arguments = @{ Configuration = $Configuration }
        if ($SkipRuntimeCheck) {
            $arguments.SkipRuntimeCheck = $true
        }

        if ($SkipNative) {
            $arguments.SkipNative = $true
        }

        if ($Wait) {
            Invoke-LongLiveScript -ScriptName (Get-CategoryScriptPath -Category 'deploy' -ScriptName 'wait-and-redeploy-host.ps1') -Arguments $arguments
        }
        else {
            Invoke-LongLiveScript -ScriptName (Get-CategoryScriptPath -Category 'deploy' -ScriptName 'redeploy-host.ps1') -Arguments $arguments
        }

        break
    }

    'host-runtime-check' {
        $arguments = @{ Configuration = $Configuration }
        Invoke-LongLiveScript -ScriptName (Get-CategoryScriptPath -Category 'verify' -ScriptName 'check-host-runtime.ps1') -Arguments $arguments
        break
    }

    'localtest-stage' {
        $arguments = @{}
        if ($DisableMissingHostReminder) {
            $arguments.DisableMissingHostReminder = $true
        }

        Invoke-LongLiveScript -ScriptName (Get-CategoryScriptPath -Category 'deploy' -ScriptName 'deploy-next-localtest.ps1') -Arguments $arguments
        break
    }

    'release-stage' {
        $arguments = @{ Configuration = $Configuration; Version = $Version }
        if ($DisableMissingHostReminder) {
            $arguments.DisableMissingHostReminder = $true
        }

        if ($IncludeNative) {
            $arguments.IncludeNative = $true
        }

        Invoke-LongLiveScript -ScriptName (Get-CategoryScriptPath -Category 'release' -ScriptName 'stage-workshop-release.ps1') -Arguments $arguments
        break
    }

    default {
        Show-Help
        break
    }
}
