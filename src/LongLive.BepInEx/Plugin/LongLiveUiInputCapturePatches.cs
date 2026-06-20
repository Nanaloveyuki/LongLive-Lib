using HarmonyLib;

namespace LongLive.BepInEx.Plugin;

[HarmonyPatch(typeof(CamaraFollow), "Update")]
internal static class LongLiveUiInputCaptureCamaraFollowPatch
{
    private static bool Prefix()
    {
        return !LongLiveUiController.IsInputCaptureActive;
    }
}

[HarmonyPatch(typeof(SeaMapUI), "Update")]
internal static class LongLiveUiInputCaptureSeaMapUiPatch
{
    private static bool Prefix()
    {
        return !LongLiveUiController.IsInputCaptureActive;
    }
}
