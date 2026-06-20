param()

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$servicePath = Join-Path $repoRoot 'src\LongLive.BepInEx\Plugin\Search\LongLivePinyinSearchService.cs'
$wordPath = Join-Path $repoRoot 'src\LongLive.BepInEx\Plugin\Search\Resources\mandarin\word.txt'
$phrasesPath = Join-Path $repoRoot 'src\LongLive.BepInEx\Plugin\Search\Resources\mandarin\phrases_dict.txt'
$phrasesMapPath = Join-Path $repoRoot 'src\LongLive.BepInEx\Plugin\Search\Resources\mandarin\phrases_map.txt'

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ('longlive-pinyin-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $projectPath = Join-Path $tempDir 'PinyinSearchTest.csproj'
    $programPath = Join-Path $tempDir 'Program.cs'
    $serviceCopyPath = Join-Path $tempDir 'LongLivePinyinSearchService.cs'

    $program = @'
using System;

namespace LongLive.BepInEx.Plugin
{
    public sealed class LongLivePlugin
    {
        public static LongLivePlugin? Instance { get; set; }

        public static TestLogSource? LogSource { get; set; }

        public TestOptions Options { get; } = new TestOptions();
    }

    public sealed class TestOptions
    {
        public TestConfigEntry<bool> EnableTuJianPinyinSearch { get; } = new TestConfigEntry<bool>(true);

        public TestConfigEntry<bool> EnableDebugLogging { get; } = new TestConfigEntry<bool>(false);
    }

    public sealed class TestConfigEntry<T>
    {
        public TestConfigEntry(T value)
        {
            Value = value;
        }

        public T Value { get; set; }
    }

    public sealed class TestLogSource
    {
        public void LogInfo(string message)
        {
        }
    }
}

internal static class PinyinSearchTestProgram
{
    private static void AssertExpanded(string input, params string[] expected)
    {
        var actual = LongLive.BepInEx.Plugin.LongLivePinyinSearchService.ExpandQueryTerms(new[] { input });
        if (actual.Length != expected.Length)
        {
            throw new Exception("Expand length mismatch. input=" + input + ", actual=" + string.Join("|", actual) + ", expected=" + string.Join("|", expected));
        }

        for (var index = 0; index < actual.Length; index++)
        {
            if (!string.Equals(actual[index], expected[index], StringComparison.Ordinal))
            {
                throw new Exception("Expand mismatch. input=" + input + ", actual=" + string.Join("|", actual) + ", expected=" + string.Join("|", expected));
            }
        }
    }

    private static void AssertMatch(string source, string query)
    {
        if (!LongLive.BepInEx.Plugin.LongLivePinyinSearchService.TryMatches(source, query, out var matched) || !matched)
        {
            throw new Exception("Expected match: " + source + " / " + query);
        }
    }

    private static void AssertNoMatch(string source, string query)
    {
        if (!LongLive.BepInEx.Plugin.LongLivePinyinSearchService.TryMatches(source, query, out var matched) || matched)
        {
            throw new Exception("Expected no match: " + source + " / " + query);
        }
    }

    public static void Main()
    {
        LongLive.BepInEx.Plugin.LongLivePlugin.Instance = new LongLive.BepInEx.Plugin.LongLivePlugin();
        LongLive.BepInEx.Plugin.LongLivePlugin.LogSource = new LongLive.BepInEx.Plugin.TestLogSource();

        AssertMatch("\u97f3\u4e50", "yinyue");
        AssertMatch("\u97f3\u4e50", "yy");
        AssertMatch("\u89c9\u5f97", "juede");
        AssertMatch("\u957f\u751f", "changsheng");
        AssertMatch("\u957f\u751f", "zhangsheng");
        AssertMatch("\u8fd8\u4e61", "huanxiang");
        AssertMatch("\u4e00\u76ee\u5341\u884c", "yimushihang");
        AssertMatch("\u4e00\u76ee\u5341\u884c", "ymsh");
        AssertMatch("\u89c5\u957f\u751f", "michangsheng");
        AssertMatch("\u89c5\u957f\u751f", "mi\u3000changsheng");
        AssertMatch("\u4ea4\u54cd\u4e50", "jiaoxiangyue");
        AssertMatch("\u97f3\u4e50", "yin yue");
        AssertExpanded("yin,yue", "yin", "yue");
        AssertExpanded("yin/yue", "yin", "yue");
        AssertExpanded("yin|yue", "yin", "yue");
        AssertExpanded("yin;yue", "yin", "yue");
        AssertNoMatch("\u89c5\u957f\u751f", "mizhansheng");

        Console.WriteLine("LongLivePinyinSearchService tests passed.");
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
    <Compile Include="LongLivePinyinSearchService.cs" />
    <EmbeddedResource Include="$wordPath" LogicalName="LongLive.BepInEx.Plugin.Search.Resources.mandarin.word.txt" />
    <EmbeddedResource Include="$phrasesPath" LogicalName="LongLive.BepInEx.Plugin.Search.Resources.mandarin.phrases_dict.txt" />
    <EmbeddedResource Include="$phrasesMapPath" LogicalName="LongLive.BepInEx.Plugin.Search.Resources.mandarin.phrases_map.txt" />
  </ItemGroup>
</Project>
"@

    [System.IO.File]::WriteAllText($projectPath, $project, [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllText($programPath, $program, [System.Text.Encoding]::UTF8)
    Copy-Item -LiteralPath $servicePath -Destination $serviceCopyPath

    dotnet run --project $projectPath --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Pinyin search test execution failed with exit code $LASTEXITCODE"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
