param()
$target = Join-Path $PSScriptRoot 'release\\stage-workshop-release.ps1'
& $target @args
exit $LASTEXITCODE