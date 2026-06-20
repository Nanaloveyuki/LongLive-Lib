param()
$target = Join-Path $PSScriptRoot 'deploy\\deploy-host.ps1'
& $target @args
exit $LASTEXITCODE