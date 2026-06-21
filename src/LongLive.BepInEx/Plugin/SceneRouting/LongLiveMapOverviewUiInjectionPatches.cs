using HarmonyLib;

namespace LongLive.BepInEx.Plugin;

[HarmonyPatch(typeof(UIMapNingZhou), nameof(UIMapNingZhou.Init))]
internal static class LongLiveMapOverviewUiInjectionNingZhouInitPatch
{
    public static void Postfix(UIMapNingZhou __instance)
    {
        LongLiveMapOverviewUiInjectionRuntime.EnsureInjectedNingZhouNodes(__instance, "UIMapNingZhou.Init");
    }
}

[HarmonyPatch(typeof(UIMapNingZhou), nameof(UIMapNingZhou.Show))]
internal static class LongLiveMapOverviewUiInjectionNingZhouShowPatch
{
    public static void Postfix(UIMapNingZhou __instance)
    {
        LongLiveMapOverviewUiInjectionRuntime.EnsureInjectedNingZhouNodes(__instance, "UIMapNingZhou.Show");
    }
}

[HarmonyPatch(typeof(UIMapNingZhou), nameof(UIMapNingZhou.OnNodeClick))]
internal static class LongLiveMapOverviewUiInjectionNingZhouClickPatch
{
    public static bool Prefix(UIMapNingZhouNode node)
    {
        return !LongLiveMapOverviewUiInjectionRuntime.TryHandleInjectedNodeClick(node);
    }
}
