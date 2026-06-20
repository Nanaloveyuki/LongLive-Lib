using HarmonyLib;

namespace LongLive.BepInEx.Plugin;

[HarmonyPatch(typeof(UIPopTip), nameof(UIPopTip.Pop), new[] { typeof(string), typeof(PopTipIconType) })]
internal static class LongLivePopTipOptimizationPatch
{
    private static bool Prepare()
    {
        return LongLivePopTipOptimizationRuntime.IsEnabled;
    }

    private static bool Prefix(string msg, PopTipIconType iconType)
    {
        return !LongLivePopTipOptimizationRuntime.TryInterceptPop(msg, iconType, null);
    }
}

[HarmonyPatch(typeof(UIPopTip), nameof(UIPopTip.Pop), new[] { typeof(string), typeof(string), typeof(PopTipIconType) })]
internal static class LongLivePopTipOptimizationWithSoundPatch
{
    private static bool Prepare()
    {
        return LongLivePopTipOptimizationRuntime.IsEnabled;
    }

    private static bool Prefix(string msg, string sound, PopTipIconType iconType)
    {
        return !LongLivePopTipOptimizationRuntime.TryInterceptPop(msg, iconType, sound);
    }
}
