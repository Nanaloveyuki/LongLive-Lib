param()
$target = Join-Path $PSScriptRoot 'deploy\\build-host.ps1'
& $target @args
exit $LASTEXITCODE