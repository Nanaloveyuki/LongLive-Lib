param()
$target = Join-Path $PSScriptRoot 'test\\test-poptip-runtime-access.ps1'
& $target @args
exit $LASTEXITCODE