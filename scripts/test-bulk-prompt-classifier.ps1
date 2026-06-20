param()
$target = Join-Path $PSScriptRoot 'test\\test-bulk-prompt-classifier.ps1'
& $target @args
exit $LASTEXITCODE