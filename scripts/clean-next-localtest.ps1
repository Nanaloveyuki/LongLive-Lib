param()
$target = Join-Path $PSScriptRoot 'deploy\\clean-next-localtest.ps1'
& $target @args
exit $LASTEXITCODE