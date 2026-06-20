using System;
using HarmonyLib;
using YSGame.TuJian;

namespace LongLive.BepInEx.Plugin;

[HarmonyPatch(typeof(TuJianSearcher), nameof(TuJianSearcher.Search))]
internal static class LongLiveTuJianSearchPatch
{
    private static bool Prepare()
    {
        return true;
    }

    private static bool Prefix(TuJianSearcher __instance, string str)
    {
        if (!LongLivePinyinSearchService.IsEnabled)
        {
            return true;
        }

        var rawTerms = string.IsNullOrWhiteSpace(str)
            ? System.Array.Empty<string>()
            : str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var expandedTerms = LongLivePinyinSearchService.ExpandQueryTerms(rawTerms);

        LongLivePinyinSearchService.LogQueryExpansion(str ?? string.Empty, rawTerms, expandedTerms);

        AccessTools.Field(typeof(TuJianSearcher), "searchStrs")?.SetValue(__instance, rawTerms.Length == 0 ? null : rawTerms);
        LongLiveTuJianSearchStateStore.SetExpandedTerms(__instance, expandedTerms.Length == 0 ? null : expandedTerms);
        TuJianManager.Inst.NeedRefreshDataList = true;
        TuJianManager.TabDict[TuJianManager.Inst.NowTuJianTab].RefreshPanel(false);
        return false;
    }
}

[HarmonyPatch(typeof(TuJianSearcher), nameof(TuJianSearcher.IsContansSearch))]
internal static class LongLiveTuJianPinyinSearchPatch
{
    private static bool Prepare()
    {
        return true;
    }

    private static bool Prefix(TuJianSearcher __instance, string str, ref bool __result)
    {
        if (!LongLivePinyinSearchService.IsEnabled)
        {
            return true;
        }

        var searchStrs = LongLiveTuJianSearchStateStore.GetExpandedTerms(__instance)
            ?? LongLivePinyinSearchService.ExpandQueryTerms(AccessTools.Field(typeof(TuJianSearcher), "searchStrs")?.GetValue(__instance) as string[]);
        if (searchStrs == null || searchStrs.Length == 0)
        {
            return true;
        }

        foreach (var value in searchStrs)
        {
            if (!LongLivePinyinSearchService.TryMatches(str, value, out var matched))
            {
                return true;
            }

            LongLivePinyinSearchService.LogMatchResult(str, value, matched);

            if (matched)
            {
                __result = true;
                return false;
            }
        }

        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(TuJianSearcher), nameof(TuJianSearcher.ClearSearchStrAndNoSearch))]
internal static class LongLiveTuJianClearSearchPatch
{
    private static void Postfix(TuJianSearcher __instance)
    {
        LongLiveTuJianSearchStateStore.Clear(__instance);
    }
}
