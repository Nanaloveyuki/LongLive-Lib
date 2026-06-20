param()
$target = Join-Path $PSScriptRoot 'verify\\read-host-log.ps1'
& $target @args
exit $LASTEXITCODE