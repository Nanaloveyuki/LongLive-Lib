using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLivePopTipOptimizationRuntime
{
    private const int ImmediateFlushUniqueEntryThreshold = 24;

    private static readonly List<LongLivePopTipAggregationEntry> PendingEntries = new List<LongLivePopTipAggregationEntry>();
    private static CoroutineHost? _host;
    private static Coroutine? _flushCoroutine;
    private static bool _suppressInterception;

    public static bool IsEnabled => LongLivePlugin.Instance?.Options.EnablePopTipOptimization.Value == true;

    private static float AggregationWindowSeconds
    {
        get
        {
            var configured = LongLivePlugin.Instance?.Options.PopTipAggregationWindowMs.Value ?? 500f;
            return Math.Max(0.05f, configured / 1000f);
        }
    }

    private static int FastModeThreshold
    {
        get
        {
            var configured = LongLivePlugin.Instance?.Options.PopTipFastModeThreshold.Value ?? 6;
            return Math.Max(2, configured);
        }
    }

    public static bool TryInterceptPop(string? msg, PopTipIconType iconType, string? sound)
    {
        if (!IsEnabled || _suppressInterception || string.IsNullOrWhiteSpace(msg))
        {
            return false;
        }

        if (LongLiveBulkItemUseRuntime.IsCapturingAggregationSession)
        {
            LogVerbose("pop-tip interception skipped because bulk aggregation session is active");
            return false;
        }

        EnsureHost();
        ApplyFastModeIfNeeded();

        var message = msg!.Trim();
        var now = Time.unscaledTime;
        RecordPendingEntry(message, iconType, sound, now);

        if (PendingEntries.Count >= ImmediateFlushUniqueEntryThreshold)
        {
            LogVerbose($"pop-tip optimizer immediate flush triggered: uniqueEntries={PendingEntries.Count}");
            FlushPending();
            return true;
        }

        ScheduleFlush();
        return true;
    }

    public static void OnSceneLoaded(Scene scene)
    {
        CancelScheduledFlush();
        PendingEntries.Clear();
        CleanupLivePopTips();
        LogVerbose($"pop-tip optimizer scene cleanup: scene={scene.name}");
    }

    public static void OnPluginShutdown()
    {
        CancelScheduledFlush();
        PendingEntries.Clear();
        CleanupLivePopTips();
        RestoreDefaultTimingIfPossible();
        LongLivePopTipRuntimeAccess.ClearAllTimingSnapshots();
    }

    public static void FlushPending()
    {
        if (PendingEntries.Count == 0)
        {
            return;
        }

        try
        {
            if (!LongLivePopTipRuntimeAccess.TryGetSingleton(out var popTipType, out var inst))
            {
                return;
            }

            _suppressInterception = true;

            foreach (var entry in PendingEntries.ToArray())
            {
                var message = entry.BuildMessage();
                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                var popWithSound = AccessTools.Method(popTipType, "Pop", new[] { typeof(string), typeof(string), typeof(PopTipIconType) });
                if (popWithSound != null)
                {
                    popWithSound.Invoke(inst, new object[] { message, entry.Sound ?? string.Empty, entry.IconType });
                    continue;
                }

                var popWithoutSound = AccessTools.Method(popTipType, "Pop", new[] { typeof(string), typeof(PopTipIconType) });
                popWithoutSound?.Invoke(inst, new object[] { message, entry.IconType });
            }

            LogVerbose($"pop-tip optimizer flushed {PendingEntries.Count} aggregated entr{(PendingEntries.Count == 1 ? "y" : "ies")}");
        }
        catch (Exception exception)
        {
            LogVerbose("pop-tip optimizer flush failed: " + exception.GetType().Name + ": " + exception.Message);
        }
        finally
        {
            PendingEntries.Clear();
            _suppressInterception = false;
            _flushCoroutine = null;
            RestoreDefaultTimingIfPossible();
        }
    }

    private static void ScheduleFlush()
    {
        if (_host == null)
        {
            return;
        }

        if (_flushCoroutine != null)
        {
            _host.StopCoroutine(_flushCoroutine);
        }

        _flushCoroutine = _host.StartCoroutine(FlushAfterWindow());
    }

    private static IEnumerator FlushAfterWindow()
    {
        yield return new WaitForSecondsRealtime(AggregationWindowSeconds);
        FlushPending();
    }

    private static void RecordPendingEntry(string message, PopTipIconType iconType, string? sound, float now)
    {
        var merged = LongLivePopTipAggregation.Record(PendingEntries, message, iconType, sound, now, AggregationWindowSeconds);
        if (!merged)
        {
            var latestEntry = PendingEntries[PendingEntries.Count - 1];
            if (latestEntry.HasNumericSuffix)
            {
                LogVerbose($"pop-tip numeric aggregation started: prefix={latestEntry.Prefix}, value={latestEntry.NumericValue}, suffix={latestEntry.Suffix}");
                return;
            }

            LogVerbose($"pop-tip aggregation queued literal: message={message}");
            return;
        }

        var mergedEntry = PendingEntries.LastOrDefault(candidate =>
            candidate.IconType == iconType &&
            string.Equals(candidate.Sound, sound, StringComparison.Ordinal) &&
            now - candidate.LastSeenAt <= AggregationWindowSeconds);

        if (mergedEntry != null && mergedEntry.HasNumericSuffix)
        {
            LogVerbose($"pop-tip numeric aggregation merged: prefix={mergedEntry.Prefix}, total={mergedEntry.NumericValue}, count={mergedEntry.Count}");
            return;
        }

        if (mergedEntry != null)
        {
            LogVerbose($"pop-tip literal aggregation merged: message={message}, count={mergedEntry.Count}");
        }
    }

    private static void EnsureHost()
    {
        if (_host != null)
        {
            return;
        }

        var hostObject = new GameObject("LongLivePopTipOptimizationHost");
        UnityEngine.Object.DontDestroyOnLoad(hostObject);
        _host = hostObject.AddComponent<CoroutineHost>();
    }

    private static void ApplyFastModeIfNeeded()
    {
        Type? popTipType = null;
        object? inst = null;

        try
        {
            if (!LongLivePopTipRuntimeAccess.TryGetSingleton(out popTipType, out inst))
            {
                return;
            }

            LongLivePopTipRuntimeAccess.CaptureTimingSnapshotIfNeeded(popTipType, inst);

            var waitForShow = LongLivePopTipRuntimeAccess.GetWaitForShow(popTipType, inst);
            var totalCount = LongLivePopTipRuntimeAccess.GetCollectionCount(waitForShow)
                + LongLivePopTipRuntimeAccess.GetTipsCount(popTipType, inst)
                + PendingEntries.Count;

            if (totalCount < FastModeThreshold)
            {
                RestoreTimingFields(popTipType, inst);
                return;
            }

            LongLivePopTipRuntimeAccess.SetTimingFields(popTipType, inst, 0.12f, Math.Min(totalCount * 0.08f + 0.45f, 1.2f), 0.08f);
            LogVerbose($"pop-tip fast mode applied: totalCount={totalCount}");
        }
        catch (Exception exception)
        {
            if (popTipType != null && inst != null)
            {
                RestoreTimingFields(popTipType, inst);
            }

            LogVerbose("pop-tip optimizer fast-mode update failed: " + exception.GetType().Name + ": " + exception.Message);
        }
    }

    private static void CleanupLivePopTips()
    {
        Type? popTipType = null;
        object? inst = null;

        try
        {
            if (!LongLivePopTipRuntimeAccess.TryGetSingleton(out popTipType, out inst))
            {
                return;
            }

            LongLivePopTipRuntimeAccess.CaptureTimingSnapshotIfNeeded(popTipType, inst);

            var waitForShow = LongLivePopTipRuntimeAccess.GetWaitForShow(popTipType, inst);
            LongLivePopTipRuntimeAccess.ClearWaitForShow(waitForShow);
            LongLivePopTipRuntimeAccess.SetTimingFields(popTipType, inst, 0f, 0f, 0f);
            LongLivePopTipRuntimeAccess.ClearAddItemMergeDictionary(popTipType, inst);

            var staleObjects = LongLivePopTipRuntimeAccess.CollectAndClearTips(popTipType, inst);
            LongLivePopTipRuntimeAccess.DestroyTipObjects(staleObjects);

        }
        catch (Exception exception)
        {
            LogVerbose("pop-tip optimizer cleanup failed: " + exception.GetType().Name + ": " + exception.Message);
        }
        finally
        {
            if (popTipType != null && inst != null)
            {
                RestoreTimingFields(popTipType, inst);
            }
        }
    }

    private static void RestoreDefaultTimingIfPossible()
    {
        try
        {
            if (!LongLivePopTipRuntimeAccess.TryGetSingleton(out var popTipType, out var inst))
            {
                return;
            }

            RestoreTimingFields(popTipType, inst);
        }
        catch (Exception exception)
        {
            LogVerbose("pop-tip optimizer timing restore failed: " + exception.GetType().Name + ": " + exception.Message);
        }
    }

    private static void RestoreTimingFields(Type popTipType, object inst)
    {
        if (!LongLivePopTipRuntimeAccess.TryRestoreTimingSnapshot(popTipType, inst))
        {
            return;
        }
    }

    private static void CancelScheduledFlush()
    {
        if (_flushCoroutine != null && _host != null)
        {
            _host.StopCoroutine(_flushCoroutine);
        }

        _flushCoroutine = null;
    }

    private static void LogVerbose(string message)
    {
        if (LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true)
        {
            LongLivePlugin.LogSource?.LogInfo("[PopTipOptimization] " + message);
        }
    }

    private sealed class CoroutineHost : MonoBehaviour
    {
    }
}
