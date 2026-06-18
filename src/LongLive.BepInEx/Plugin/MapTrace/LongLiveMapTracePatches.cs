using HarmonyLib;

namespace LongLive.BepInEx.Plugin;

[HarmonyPatch(typeof(Tools), nameof(Tools.loadMapScenes))]
internal static class LongLiveMapTraceToolsLoadMapScenesPatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceToolsLoadMapScenesPatch));
    }

    public static void Prefix(string name, bool LastSceneIsValue)
    {
        LongLiveMapTraceRuntime.LogSceneLoadRequest(name, LastSceneIsValue);
    }
}

[HarmonyPatch(typeof(AllMapManage), "Awake")]
internal static class LongLiveMapTraceAllMapManageAwakePatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceAllMapManageAwakePatch));
    }

    public static void Postfix(AllMapManage __instance)
    {
        LongLiveMapTraceRuntime.LogAllMapManageSnapshot(__instance, "AllMapManage.Awake", force: true);
    }
}

[HarmonyPatch(typeof(AllMapManage), nameof(AllMapManage.RefreshLuDian))]
internal static class LongLiveMapTraceAllMapManageRefreshLuDianPatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceAllMapManageRefreshLuDianPatch));
    }

    public static void Postfix(AllMapManage __instance)
    {
        LongLiveMapTraceRuntime.LogAllMapManageSnapshot(__instance, "AllMapManage.RefreshLuDian", force: false);
    }
}

[HarmonyPatch(typeof(BaseMapCompont), nameof(BaseMapCompont.StartSeting))]
internal static class LongLiveMapTraceBaseMapCompontStartSetingPatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceBaseMapCompontStartSetingPatch));
    }

    public static void Postfix(BaseMapCompont __instance)
    {
        LongLiveMapTraceRuntime.LogNodeRegistration(__instance, "BaseMapCompont.StartSeting");
    }
}

[HarmonyPatch(typeof(BaseMapCompont), nameof(BaseMapCompont.setAvatarNowMapIndex))]
internal static class LongLiveMapTraceBaseMapCompontSetAvatarNowMapIndexPatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceBaseMapCompontSetAvatarNowMapIndexPatch));
    }

    public static void Postfix(BaseMapCompont __instance)
    {
        LongLiveMapTraceRuntime.LogNodeMove(__instance, "BaseMapCompont.setAvatarNowMapIndex");
    }
}

[HarmonyPatch(typeof(MapInstComport), nameof(MapInstComport.setAvatarNowMapIndex))]
internal static class LongLiveMapTraceMapInstComportSetAvatarNowMapIndexPatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceMapInstComportSetAvatarNowMapIndexPatch));
    }

    public static void Postfix(MapInstComport __instance)
    {
        LongLiveMapTraceRuntime.LogNodeMove(__instance, "MapInstComport.setAvatarNowMapIndex");
    }
}

[HarmonyPatch(typeof(UIMapPanel), nameof(UIMapPanel.OpenDefaultMap))]
internal static class LongLiveMapTraceUiMapPanelOpenDefaultMapPatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceUiMapPanelOpenDefaultMapPatch));
    }

    public static void Postfix(UIMapPanel __instance)
    {
        LongLiveMapTraceRuntime.LogUiMapPanelSnapshot(__instance, "UIMapPanel.OpenDefaultMap", force: true);
    }
}

[HarmonyPatch(typeof(UIMapPanel), nameof(UIMapPanel.OpenMap))]
internal static class LongLiveMapTraceUiMapPanelOpenMapPatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceUiMapPanelOpenMapPatch));
    }

    public static void Postfix(UIMapPanel __instance, MapArea area, UIMapState state)
    {
        LongLiveMapTraceRuntime.LogUiMapPanelSnapshot(__instance, $"UIMapPanel.OpenMap(area={area}, state={state})", force: true);
    }
}

[HarmonyPatch(typeof(UIMapPanel), nameof(UIMapPanel.OpenHighlight))]
internal static class LongLiveMapTraceUiMapPanelOpenHighlightPatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceUiMapPanelOpenHighlightPatch));
    }

    public static void Prefix(int id)
    {
        LongLiveMapTraceRuntime.Log($"UIMapPanel.OpenHighlight prefix: id={id}");
    }
}

[HarmonyPatch(typeof(UIMapPanel), "ShowPanel")]
internal static class LongLiveMapTraceUiMapPanelShowPanelPatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceUiMapPanelShowPanelPatch));
    }

    public static void Postfix(UIMapPanel __instance)
    {
        LongLiveMapTraceRuntime.LogUiMapPanelSnapshot(__instance, "UIMapPanel.ShowPanel", force: true);
    }
}

[HarmonyPatch(typeof(UIMapNingZhou), nameof(UIMapNingZhou.Show))]
internal static class LongLiveMapTraceUiMapNingZhouShowPatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceUiMapNingZhouShowPatch));
    }

    public static void Postfix(UIMapNingZhou __instance)
    {
        LongLiveMapTraceRuntime.LogUiMapNingZhouSnapshot(__instance, "UIMapNingZhou.Show", force: true);
    }
}

[HarmonyPatch(typeof(UIMapSea), nameof(UIMapSea.Show))]
internal static class LongLiveMapTraceUiMapSeaShowPatch
{
    public static bool Prepare()
    {
        return LongLiveMapTraceRuntime.Prepare(nameof(LongLiveMapTraceUiMapSeaShowPatch));
    }

    public static void Postfix(UIMapSea __instance)
    {
        LongLiveMapTraceRuntime.LogUiMapSeaSnapshot(__instance, "UIMapSea.Show", force: true);
    }
}
