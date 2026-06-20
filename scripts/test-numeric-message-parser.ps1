param()
$target = Join-Path $PSScriptRoot 'test\\test-numeric-message-parser.ps1'
& $target @args
exit $LASTEXITCODE