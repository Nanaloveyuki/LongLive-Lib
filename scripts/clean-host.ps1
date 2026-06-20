param()
$target = Join-Path $PSScriptRoot 'deploy\\clean-host.ps1'
& $target @args
exit $LASTEXITCODE