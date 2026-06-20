param()
$target = Join-Path $PSScriptRoot 'deploy\\deploy-next-localtest.ps1'
& $target @args
exit $LASTEXITCODE