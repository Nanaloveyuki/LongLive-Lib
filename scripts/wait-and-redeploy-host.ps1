param()
$target = Join-Path $PSScriptRoot 'deploy\\wait-and-redeploy-host.ps1'
& $target @args
exit $LASTEXITCODE