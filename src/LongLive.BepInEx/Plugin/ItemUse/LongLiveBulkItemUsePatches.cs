using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using Bag;

namespace LongLive.BepInEx.Plugin;

[HarmonyPatch(typeof(SlotBase), nameof(SlotBase.OnPointerDown))]
internal static class LongLiveBulkItemUseSlotPointerDownPatch
{
    private static bool Prepare()
    {
        return LongLiveBulkItemUseRuntime.IsEnabled;
    }

    private static void Prefix(SlotBase __instance, PointerEventData eventData)
    {
        LongLiveBulkItemUseRuntime.TryHandlePointerDown(__instance, eventData);
    }
}

[HarmonyPatch(typeof(SlotBase), nameof(SlotBase.OnPointerUp))]
internal static class LongLiveBulkItemUseSlotPointerUpPatch
{
    private static bool Prepare()
    {
        return LongLiveBulkItemUseRuntime.IsEnabled;
    }

    private static bool Prefix(SlotBase __instance, PointerEventData eventData)
    {
        return !LongLiveBulkItemUseRuntime.TryHandlePointerUp(__instance, eventData);
    }
}

[HarmonyPatch(typeof(UIPopTip), nameof(UIPopTip.Pop), new[] { typeof(string), typeof(PopTipIconType) })]
internal static class LongLiveBulkItemUsePopTipPatch
{
    private static bool Prepare()
    {
        return LongLiveBulkItemUseRuntime.IsEnabled;
    }

    private static bool Prefix(string msg, PopTipIconType iconType)
    {
        return !LongLiveBulkItemUseRuntime.TryAggregatePopTip(msg, iconType, null);
    }
}

[HarmonyPatch(typeof(UIPopTip), nameof(UIPopTip.Pop), new[] { typeof(string), typeof(string), typeof(PopTipIconType) })]
internal static class LongLiveBulkItemUsePopTipWithSoundPatch
{
    private static bool Prepare()
    {
        return LongLiveBulkItemUseRuntime.IsEnabled;
    }

    private static bool Prefix(string msg, string sound, PopTipIconType iconType)
    {
        return !LongLiveBulkItemUseRuntime.TryAggregatePopTip(msg, iconType, sound);
    }
}
