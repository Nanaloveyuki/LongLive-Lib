param()
$target = Join-Path $PSScriptRoot 'test\\test-pop-tip-aggregation.ps1'
& $target @args
exit $LASTEXITCODE