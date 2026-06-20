using System;
using System.Runtime.CompilerServices;
using YSGame.TuJian;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveTuJianSearchStateStore
{
    private static readonly ConditionalWeakTable<TuJianSearcher, SearchState> States = new ConditionalWeakTable<TuJianSearcher, SearchState>();

    public static void SetExpandedTerms(TuJianSearcher searcher, string[]? expandedTerms)
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

    public static string[]? GetExpandedTerms(TuJianSearcher searcher)
    {
        if (searcher == null)
        {
            return null;
        }

        return States.TryGetValue(searcher, out var state) ? state.ExpandedTerms : null;
    }

    public static void Clear(TuJianSearcher searcher)
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

        public string[] ExpandedTerms { get; }
    }
}
