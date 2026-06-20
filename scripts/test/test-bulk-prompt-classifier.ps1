param()

$ErrorActionPreference = 'Stop'

$program = @'
using System;
using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin
{
internal enum LongLiveBulkUsePromptMarker
{
    CultivationGain,
    DanFangUnlock,
    SeaExplorationGain,
    SkillUnlock,
    StaticSkillUnlock,
    HerbEncyclopediaUnlock,
    HerbOriginUnlock,
    TemporaryItemBuffUnlock,
    FaceCustomizationUnlock,
}

internal static class LongLiveBulkItemUsePromptClassifier
{
    public static void Observe(string message, ISet<LongLiveBulkUsePromptMarker> markers)
    {
        if (markers == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var normalized = message.Trim();

        if (normalized.StartsWith("\u4f60\u7684\u4fee\u4e3a\u63d0\u5347\u4e86", StringComparison.Ordinal) || normalized.StartsWith("\u4f60\u7684\u4fee\u4e3a\u964d\u4f4e\u4e86", StringComparison.Ordinal))
        {
            markers.Add(LongLiveBulkUsePromptMarker.CultivationGain);
        }

        if (normalized.StartsWith("\u5b66\u4f1a\u4e86", StringComparison.Ordinal) && normalized.IndexOf("\u70bc\u5236\u914d\u65b9", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.DanFangUnlock);
        }

        if (normalized.StartsWith("\u5b66\u4f1a\u4e86", StringComparison.Ordinal) && normalized.IndexOf("\u795e\u901a", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.SkillUnlock);
        }

        if (normalized.StartsWith("\u5b66\u4f1a\u4e86", StringComparison.Ordinal) && normalized.IndexOf("\u529f\u6cd5", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.StaticSkillUnlock);
        }

        if (normalized.IndexOf("\u8349\u836f\u56fe\u9274", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.HerbEncyclopediaUnlock);
        }

        if (normalized.IndexOf("\u8349\u836f\u4ea7\u5730", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.HerbOriginUnlock);
        }

        if (normalized.StartsWith("\u5bf9", StringComparison.Ordinal) && normalized.IndexOf("\u7684\u63a2\u7d22\u5ea6\u63d0\u5347\u4e86", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.SeaExplorationGain);
        }

        if (normalized.IndexOf("\u4e34\u65f6\u7269\u54c1\u589e\u76ca", StringComparison.Ordinal) >= 0)
        {
            markers.Add(LongLiveBulkUsePromptMarker.TemporaryItemBuffUnlock);
        }

        if (string.Equals(normalized, "\u4f60\u89e3\u9501\u4e86\u89d2\u8272\u5f62\u8c61\u8c03\u6574", StringComparison.Ordinal))
        {
            markers.Add(LongLiveBulkUsePromptMarker.FaceCustomizationUnlock);
        }
    }
}

internal static class BulkPromptClassifierTestProgram
{
    private static void AssertHas(string message, LongLiveBulkUsePromptMarker marker)
    {
        var markers = new HashSet<LongLiveBulkUsePromptMarker>();
        LongLiveBulkItemUsePromptClassifier.Observe(message, markers);
        if (!markers.Contains(marker))
        {
            throw new Exception("Expected marker not found. message=" + message + ", marker=" + marker);
        }
    }

    public static void Main()
    {
        AssertHas("\u4f60\u7684\u4fee\u4e3a\u63d0\u5347\u4e86120", LongLiveBulkUsePromptMarker.CultivationGain);
        AssertHas("\u5b66\u4f1a\u4e86\u4e5d\u8f6c\u4e39\u70bc\u5236\u914d\u65b9", LongLiveBulkUsePromptMarker.DanFangUnlock);
        AssertHas("\u5b66\u4f1a\u4e86\u5929\u96f7\u795e\u901a", LongLiveBulkUsePromptMarker.SkillUnlock);
        AssertHas("\u5b66\u4f1a\u4e86\u592a\u865a\u529f\u6cd5", LongLiveBulkUsePromptMarker.StaticSkillUnlock);
        AssertHas("\u4f60\u89e3\u9501\u4e86\u8349\u836f\u56fe\u9274\uff1a\u8d64\u7075\u829d", LongLiveBulkUsePromptMarker.HerbEncyclopediaUnlock);
        AssertHas("\u4f60\u638c\u63e1\u4e86\u8349\u836f\u4ea7\u5730\uff1a\u4e1c\u6d77", LongLiveBulkUsePromptMarker.HerbOriginUnlock);
        AssertHas("\u5bf9\u4e1c\u6d77\u7684\u63a2\u7d22\u5ea6\u63d0\u5347\u4e865", LongLiveBulkUsePromptMarker.SeaExplorationGain);
        AssertHas("\u4f60\u83b7\u5f97\u4e86\u4e34\u65f6\u7269\u54c1\u589e\u76ca\uff1a\u6d77\u4e0a\u4e34\u65f6\u72b6\u6001", LongLiveBulkUsePromptMarker.TemporaryItemBuffUnlock);
        AssertHas("\u4f60\u89e3\u9501\u4e86\u89d2\u8272\u5f62\u8c61\u8c03\u6574", LongLiveBulkUsePromptMarker.FaceCustomizationUnlock);

        Console.WriteLine("LongLiveBulkItemUsePromptClassifier tests passed.");
    }
}
}
'@

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ('longlive-bulk-prompt-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $programPath = Join-Path $tempDir 'BulkPromptClassifierTestProgram.cs'
    [System.IO.File]::WriteAllText($programPath, $program, [System.Text.Encoding]::UTF8)

    $compiler = Join-Path ${env:WINDIR} 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path $compiler)) {
        $compiler = Join-Path ${env:WINDIR} 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
    }

    if (-not (Test-Path $compiler)) {
        throw 'CSC compiler not found in .NET Framework directories.'
    }

    $outputPath = Join-Path $tempDir 'BulkPromptClassifierTestProgram.exe'
    & $compiler /nologo /t:exe /out:$outputPath $programPath
    if ($LASTEXITCODE -ne 0) {
        throw "Bulk prompt classifier test compilation failed with exit code $LASTEXITCODE"
    }

    & $outputPath
    if ($LASTEXITCODE -ne 0) {
        throw "Bulk prompt classifier test execution failed with exit code $LASTEXITCODE"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
