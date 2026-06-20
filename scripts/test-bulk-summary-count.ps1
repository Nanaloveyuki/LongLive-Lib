param()
$target = Join-Path $PSScriptRoot 'test\\test-bulk-summary-count.ps1'
& $target @args
exit $LASTEXITCODE