using HarmonyLib;

namespace LongLive.BepInEx.Plugin;

[HarmonyPatch(typeof(UIMapPanel), nameof(UIMapPanel.OpenDefaultMap))]
internal static class LongLiveMapOverviewCustomPageOpenDefaultMapPatch
{
    public static void Prefix(UIMapPanel __instance)
    {
        LongLiveMapOverviewCustomPageRuntime.PrepareForBuiltInPage(__instance, nameof(UIMapPanel.OpenDefaultMap));
    }
}

[HarmonyPatch(typeof(UIMapPanel), nameof(UIMapPanel.OpenMap))]
internal static class LongLiveMapOverviewCustomPageOpenMapPatch
{
    public static void Prefix(UIMapPanel __instance)
    {
        LongLiveMapOverviewCustomPageRuntime.PrepareForBuiltInPage(__instance, nameof(UIMapPanel.OpenMap));
    }
}

[HarmonyPatch(typeof(UIMapPanel), "OnNingZhouTabClick")]
internal static class LongLiveMapOverviewCustomPageOnNingZhouTabClickPatch
{
    public static void Prefix(UIMapPanel __instance)
    {
        LongLiveMapOverviewCustomPageRuntime.PrepareForBuiltInPage(__instance, "OnNingZhouTabClick");
    }
}

[HarmonyPatch(typeof(UIMapPanel), "OnSeaTabClick")]
internal static class LongLiveMapOverviewCustomPageOnSeaTabClickPatch
{
    public static void Prefix(UIMapPanel __instance)
    {
        LongLiveMapOverviewCustomPageRuntime.PrepareForBuiltInPage(__instance, "OnSeaTabClick");
    }
}

[HarmonyPatch(typeof(UIMapPanel), "ShowPanel")]
internal static class LongLiveMapOverviewCustomPageShowPanelPatch
{
    public static void Postfix(UIMapPanel __instance)
    {
        LongLiveMapOverviewCustomPageRuntime.OnPanelShow(__instance);
    }
}

[HarmonyPatch(typeof(UIMapPanel), nameof(UIMapPanel.HidePanel))]
internal static class LongLiveMapOverviewCustomPageHidePanelPatch
{
    public static void Postfix(UIMapPanel __instance)
    {
        LongLiveMapOverviewCustomPageRuntime.OnPanelHide(__instance);
    }
}
