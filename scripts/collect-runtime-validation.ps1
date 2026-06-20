param()
$target = Join-Path $PSScriptRoot 'verify\\collect-runtime-validation.ps1'
& $target @args
exit $LASTEXITCODE