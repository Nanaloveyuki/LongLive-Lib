param()
$target = Join-Path $PSScriptRoot 'verify\\check-host-runtime.ps1'
& $target @args
exit $LASTEXITCODE