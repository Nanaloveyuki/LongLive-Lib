using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace LongLive.BepInEx.Plugin;

internal static class LongLivePopTipRuntimeAccess
{
    private static ConditionalWeakTable<object, TimingSnapshot> TimingSnapshots = new ConditionalWeakTable<object, TimingSnapshot>();

    public static bool TryGetSingleton(out Type popTipType, out object inst)
    {
        popTipType = AccessTools.TypeByName("UIPopTip")!;
        inst = null!;
        if (popTipType == null)
        {
            return false;
        }

        var candidate = AccessTools.Field(popTipType, "Inst")?.GetValue(null)
            ?? AccessTools.Property(popTipType, "Inst")?.GetValue(null, null);
        if (candidate == null)
        {
            return false;
        }

        inst = candidate;
        return true;
    }

    public static object? GetWaitForShow(Type popTipType, object inst)
    {
        return AccessTools.Field(popTipType, "WaitForShow")?.GetValue(inst);
    }

    public static void ClearWaitForShow(object? waitForShow)
    {
        AccessTools.Method(waitForShow?.GetType(), "Clear", Type.EmptyTypes)?.Invoke(waitForShow, Array.Empty<object>());
    }

    public static void SetTimingFields(Type popTipType, object inst, float minCd, float tweenDestroyCd, float addItemMergeCd)
    {
        AccessTools.Field(popTipType, "minCD")?.SetValue(inst, minCd);
        AccessTools.Field(popTipType, "tweenDestoryCD")?.SetValue(inst, tweenDestroyCd);
        AccessTools.Field(popTipType, "addItemMergeCD")?.SetValue(inst, addItemMergeCd);
    }

    public static float ReadTimingField(Type popTipType, object inst, string fieldName)
    {
        var value = AccessTools.Field(popTipType, fieldName)?.GetValue(inst);
        if (value is float floatValue)
        {
            return floatValue;
        }

        return 0f;
    }

    public static void CaptureTimingSnapshotIfNeeded(Type popTipType, object inst)
    {
        if (TimingSnapshots.TryGetValue(inst, out _))
        {
            return;
        }

        TimingSnapshots.Add(inst, new TimingSnapshot(
            ReadTimingField(popTipType, inst, "minCD"),
            ReadTimingField(popTipType, inst, "tweenDestoryCD"),
            ReadTimingField(popTipType, inst, "addItemMergeCD")));
    }

    public static bool TryRestoreTimingSnapshot(Type popTipType, object inst)
    {
        if (!TimingSnapshots.TryGetValue(inst, out var snapshot))
        {
            return false;
        }

        SetTimingFields(popTipType, inst, snapshot.MinCd, snapshot.TweenDestroyCd, snapshot.AddItemMergeCd);
        return true;
    }

    public static void ClearTimingSnapshot(object inst)
    {
        if (inst == null)
        {
            return;
        }

        TimingSnapshots.Remove(inst);
    }

    public static void ClearAllTimingSnapshots()
    {
        TimingSnapshots = new ConditionalWeakTable<object, TimingSnapshot>();
    }

    public static void ClearAddItemMergeDictionary(Type popTipType, object inst)
    {
        var addItemMergeDict = AccessTools.Field(popTipType, "addItemMergeMsgDict")?.GetValue(inst) as IDictionary;
        addItemMergeDict?.Clear();
    }

    public static int GetCollectionCount(object? collection)
    {
        if (collection is ICollection typedCollection)
        {
            return typedCollection.Count;
        }

        return 0;
    }

    public static int GetTipsCount(Type popTipType, object inst)
    {
        var tips = AccessTools.Field(popTipType, "Tips")?.GetValue(inst) as ICollection;
        return tips?.Count ?? 0;
    }

    public static List<object> CollectAndClearTips(Type popTipType, object inst)
    {
        var staleObjects = new List<object>();
        var tips = AccessTools.Field(popTipType, "Tips")?.GetValue(inst) as IList;
        if (tips == null)
        {
            return staleObjects;
        }

        foreach (var entry in tips)
        {
            if (entry != null)
            {
                staleObjects.Add(entry);
            }
        }

        tips.Clear();
        return staleObjects;
    }

    public static void DestroyTipObjects(IEnumerable<object> tips)
    {
        foreach (var entry in tips)
        {
            if (entry is Component tipComponent)
            {
                UnityEngine.Object.Destroy(tipComponent.gameObject);
            }
        }
    }

    private sealed class TimingSnapshot
    {
        public TimingSnapshot(float minCd, float tweenDestroyCd, float addItemMergeCd)
        {
            MinCd = minCd;
            TweenDestroyCd = tweenDestroyCd;
            AddItemMergeCd = addItemMergeCd;
        }

        public float MinCd { get; }

        public float TweenDestroyCd { get; }

        public float AddItemMergeCd { get; }
    }
}
