param()

$ErrorActionPreference = 'Stop'

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ('longlive-tujian-state-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    $projectPath = Join-Path $tempDir 'TuJianSearchStateTest.csproj'
    $programPath = Join-Path $tempDir 'Program.cs'

    $program = @'
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib
{
    public static class AccessTools
    {
        public static FieldInfo? Field(Type type, string name)
        {
            return type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}

namespace YSGame.TuJian
{
    public sealed class TuJianSearcher
    {
        private string[]? searchStrs;

        public string[]? GetRawSearchTerms()
        {
            return searchStrs;
        }

        public void ClearSearchStrAndNoSearch()
        {
            searchStrs = null;
        }
    }
}

namespace LongLive.BepInEx.Plugin
{
    internal static class LongLivePinyinSearchService
    {
        public static bool IsEnabled => true;

        public static string[] ExpandQueryTerms(IEnumerable<string>? rawTerms)
        {
            var terms = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            if (rawTerms == null)
            {
                return Array.Empty<string>();
            }

            foreach (var rawTerm in rawTerms)
            {
                if (string.IsNullOrWhiteSpace(rawTerm))
                {
                    continue;
                }

                var start = -1;
                for (var index = 0; index < rawTerm.Length; index++)
                {
                    if (char.IsWhiteSpace(rawTerm[index]) || rawTerm[index] == ',' || rawTerm[index] == ';' || rawTerm[index] == '/' || rawTerm[index] == '|')
                    {
                        AddExpandedTerm(rawTerm, start, index - start, terms, seen);
                        start = -1;
                        continue;
                    }

                    if (start < 0)
                    {
                        start = index;
                    }
                }

                AddExpandedTerm(rawTerm, start, rawTerm.Length - start, terms, seen);
            }

            return terms.ToArray();
        }

        private static void AddExpandedTerm(string rawTerm, int startIndex, int length, ICollection<string> terms, ISet<string> seen)
        {
            if (startIndex < 0 || length <= 0)
            {
                return;
            }

            var term = rawTerm.Substring(startIndex, length).Trim();
            if (string.IsNullOrWhiteSpace(term) || !seen.Add(term))
            {
                return;
            }

            terms.Add(term);
        }
    }

    internal static class LongLiveTuJianSearchStateStore
    {
        private static readonly ConditionalWeakTable<YSGame.TuJian.TuJianSearcher, SearchState> States = new ConditionalWeakTable<YSGame.TuJian.TuJianSearcher, SearchState>();

        public static void SetExpandedTerms(YSGame.TuJian.TuJianSearcher searcher, string[]? expandedTerms)
        {
            if (searcher == null)
            {
                return;
            }

            States.Remove(searcher);
            if (expandedTerms == null || expandedTerms.Length == 0)
            {
                return;
            }

            States.Add(searcher, new SearchState(expandedTerms));
        }

        public static string[]? GetExpandedTerms(YSGame.TuJian.TuJianSearcher searcher)
        {
            SearchState? state;
            return searcher != null && States.TryGetValue(searcher, out state) ? state.ExpandedTerms : null;
        }

        public static void Clear(YSGame.TuJian.TuJianSearcher searcher)
        {
            if (searcher == null)
            {
                return;
            }

            States.Remove(searcher);
        }

        private sealed class SearchState
        {
            public SearchState(string[] expandedTerms)
            {
                ExpandedTerms = expandedTerms ?? Array.Empty<string>();
            }

            public string[] ExpandedTerms { get; private set; }
        }
    }

    internal static class TuJianSearchPatchHarness
    {
        public static void ApplySearch(YSGame.TuJian.TuJianSearcher instance, string str)
        {
            var rawTerms = string.IsNullOrWhiteSpace(str)
                ? null
                : str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var expandedTerms = LongLivePinyinSearchService.ExpandQueryTerms(rawTerms ?? Array.Empty<string>());

            var searchField = HarmonyLib.AccessTools.Field(typeof(YSGame.TuJian.TuJianSearcher), "searchStrs");
            if (searchField == null)
            {
                throw new Exception("searchStrs field not found.");
            }

            searchField.SetValue(instance, rawTerms == null || rawTerms.Length == 0 ? null : rawTerms);
            LongLiveTuJianSearchStateStore.SetExpandedTerms(instance, expandedTerms.Length == 0 ? null : expandedTerms);
        }

        public static void ApplyClear(YSGame.TuJian.TuJianSearcher instance)
        {
            instance.ClearSearchStrAndNoSearch();
            LongLiveTuJianSearchStateStore.Clear(instance);
        }
    }
}

internal static class TuJianSearchStateTestProgram
{
    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }

    public static void Main()
    {
        var searcher = new YSGame.TuJian.TuJianSearcher();
        LongLive.BepInEx.Plugin.TuJianSearchPatchHarness.ApplySearch(searcher, "yin,yue mi");

        var rawTerms = searcher.GetRawSearchTerms();
        Assert(rawTerms != null && rawTerms.Length == 2, "Expected raw terms to preserve original space split semantics.");
        if (rawTerms == null)
        {
            throw new Exception("Raw terms unexpectedly null after assertion.");
        }

        Assert(rawTerms[0] == "yin,yue" && rawTerms[1] == "mi", "Expected raw searchStrs to keep comma-containing token intact.");

        var expandedTerms = LongLive.BepInEx.Plugin.LongLiveTuJianSearchStateStore.GetExpandedTerms(searcher);
        Assert(expandedTerms != null && expandedTerms.Length == 3, "Expected expanded terms to include separator-expanded tokens.");
        if (expandedTerms == null)
        {
            throw new Exception("Expanded terms unexpectedly null after assertion.");
        }

        Assert(expandedTerms[0] == "yin" && expandedTerms[1] == "yue" && expandedTerms[2] == "mi", "Expanded term order mismatch.");

        LongLive.BepInEx.Plugin.TuJianSearchPatchHarness.ApplyClear(searcher);
        Assert(searcher.GetRawSearchTerms() == null, "Expected raw searchStrs to clear.");
        Assert(LongLive.BepInEx.Plugin.LongLiveTuJianSearchStateStore.GetExpandedTerms(searcher) == null, "Expected expanded terms to clear.");

        Console.WriteLine("LongLiveTuJianSearchState tests passed.");
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
        throw "TuJian search state test execution failed with exit code $LASTEXITCODE"
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }
}
