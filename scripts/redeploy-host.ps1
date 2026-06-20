param()
$target = Join-Path $PSScriptRoot 'deploy\\redeploy-host.ps1'
& $target @args
exit $LASTEXITCODE