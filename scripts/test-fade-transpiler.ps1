param()
$target = Join-Path $PSScriptRoot 'test\\test-fade-transpiler.ps1'
& $target @args
exit $LASTEXITCODE