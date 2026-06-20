param()

$ErrorActionPreference = 'Stop'

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ('longlive-bulk-summary-count-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $projectPath = Join-Path $tempDir 'BulkSummaryCountTest.csproj'
    $programPath = Join-Path $tempDir 'Program.cs'

    $program = @'
using System;

namespace LongLive.BepInEx.Plugin
{
    internal static class LongLiveBulkItemUseRuntimeHarness
    {
        private static int _activeBulkRequestedCount;
        private static int _activeBulkCompletedCount;

        public static void SetCounts(int requested, int completed)
        {
            _activeBulkRequestedCount = requested;
            _activeBulkCompletedCount = completed;
        }

        public static int ResolveEffectiveBulkSummaryCount()
        {
            if (_activeBulkCompletedCount > 0)
            {
                return _activeBulkCompletedCount;
            }

            return _activeBulkRequestedCount;
        }
    }
}

internal static class BulkSummaryCountTestProgram
{
    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new Exception(message + " expected=" + expected + " actual=" + actual);
        }
    }

    public static void Main()
    {
        LongLive.BepInEx.Plugin.LongLiveBulkItemUseRuntimeHarness.SetCounts(300, 0);
        AssertEqual(300, LongLive.BepInEx.Plugin.LongLiveBulkItemUseRuntimeHarness.ResolveEffectiveBulkSummaryCount(), "Requested count should be used before any item is actually processed.");

        LongLive.BepInEx.Plugin.LongLiveBulkItemUseRuntimeHarness.SetCounts(300, 17);
        AssertEqual(17, LongLive.BepInEx.Plugin.LongLiveBulkItemUseRuntimeHarness.ResolveEffectiveBulkSummaryCount(), "Completed count should override requested count after partial processing.");

        LongLive.BepInEx.Plugin.LongLiveBulkItemUseRuntimeHarness.SetCounts(25, 25);
        AssertEqual(25, LongLive.BepInEx.Plugin.LongLiveBulkItemUseRuntimeHarness.ResolveEffectiveBulkSummaryCount(), "Completed count should also be used for full completion.");

        Console.WriteLine("LongLiveBulkSummaryCount tests passed.");
    }
}
'@

    $project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
  </ItemGroup>
</Project>
"@

    [System.IO.File]::WriteAllText($projectPath, $project, [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllText($programPath, $program, [System.Text.Encoding]::UTF8)

    dotnet run --project $projectPath --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Bulk summary count test execution failed with exit code $LASTEXITCODE"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
