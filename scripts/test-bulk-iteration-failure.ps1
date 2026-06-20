param()
$target = Join-Path $PSScriptRoot 'test\\test-bulk-iteration-failure.ps1'
& $target @args
exit $LASTEXITCODE