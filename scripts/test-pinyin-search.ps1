param()
$target = Join-Path $PSScriptRoot 'test\\test-pinyin-search.ps1'
& $target @args
exit $LASTEXITCODE