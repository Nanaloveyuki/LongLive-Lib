param()
$target = Join-Path $PSScriptRoot 'verify\\check-host-deploy.ps1'
& $target @args
exit $LASTEXITCODE