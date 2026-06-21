using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCustomMapRuntimeActivationRuntime
{
    private static ManualLogSource? _logger;
    private static LongLiveHostOptions? _options;

    public static void Initialize(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public static LongLiveCustomMapRuntimeActivationRuntimeSnapshot CaptureSnapshot(int sampleLimit = 8)
    {
        var readiness = LongLivePluginContext.GetCustomMapRuntimeStateSnapshot(int.MaxValue).Readiness;
        var targets = new List<LongLiveCustomMapRuntimeActivationRuntimeTarget>();
        foreach (var target in readiness.Targets.Take(sampleLimit))
        {
            var hasBindableHostProxy = target.IsHostBackedScene && target.RequiresCustomActivation;
            targets.Add(new LongLiveCustomMapRuntimeActivationRuntimeTarget
            {
                SceneLogicalId = target.SceneLogicalId,
                SceneName = target.SceneName,
                OwningModId = target.OwningModId,
                DisplayName = target.DisplayName,
                MatchesActiveScene = target.MatchesActiveScene,
                RequiresCustomActivation = target.RequiresCustomActivation,
                IsHostBackedScene = target.IsHostBackedScene,
                HasBindableHostProxy = hasBindableHostProxy,
                CanPrepareHostActivationSurface = hasBindableHostProxy,
                CanBindProxyRoute = hasBindableHostProxy && target.EntryRouteResolvable && target.ReturnRouteResolvable,
                CanEnterNow = target.CanEnterNow,
                StatusCode = target.StatusCode,
                ActivationState = ResolveActivationState(target),
                Detail = target.Detail,
            });
        }

        return new LongLiveCustomMapRuntimeActivationRuntimeSnapshot
        {
            ActiveSceneName = readiness.ActiveSceneName,
            TargetCount = readiness.TargetCount,
            ActivatedTargetCount = CountBy(readiness.Targets, static target => target.CanEnterNow),
            HostBackedTargetCount = CountBy(readiness.Targets, static target => target.IsHostBackedScene),
            PendingImplementationTargetCount = CountBy(readiness.Targets, static target => target.NeedsCustomActivationImplementation),
            BlockedTargetCount = CountBy(readiness.Targets, static target => !target.CanEnterNow),
            HasActiveTarget = readiness.HasActiveTarget,
            ActiveTargetSceneLogicalId = readiness.ActiveTargetSceneLogicalId,
            ActiveActivationState = ResolveActiveActivationState(readiness),
            ActiveStatusCode = readiness.ActiveStatusCode,
            Targets = targets,
        };
    }

    public static void LogInstallerSummary()
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[CustomMapRuntimeActivation] ready: targets={snapshot.TargetCount}, activated={snapshot.ActivatedTargetCount}, hostBacked={snapshot.HostBackedTargetCount}, pending={snapshot.PendingImplementationTargetCount}, blocked={snapshot.BlockedTargetCount}, activeState={(string.IsNullOrWhiteSpace(snapshot.ActiveActivationState) ? "n/a" : snapshot.ActiveActivationState)}");
        LogVerboseSnapshot(snapshot, "installer");
    }

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[CustomMapRuntimeActivation] sceneLoaded: scene={scene.name}, mode={mode}, activeTarget={(string.IsNullOrWhiteSpace(snapshot.ActiveTargetSceneLogicalId) ? "n/a" : snapshot.ActiveTargetSceneLogicalId)}, activeState={(string.IsNullOrWhiteSpace(snapshot.ActiveActivationState) ? "n/a" : snapshot.ActiveActivationState)}, blocked={snapshot.BlockedTargetCount}, pending={snapshot.PendingImplementationTargetCount}");
        LogVerboseSnapshot(snapshot, "sceneLoaded");
    }

    private static void LogVerboseSnapshot(LongLiveCustomMapRuntimeActivationRuntimeSnapshot snapshot, string source)
    {
        if (!IsVerbose())
        {
            return;
        }

        var targets = snapshot.Targets.Count == 0
            ? "n/a"
            : string.Join(" | ", snapshot.Targets.Select(static target => target.SceneLogicalId + ":state=" + target.ActivationState + ",status=" + target.StatusCode + ",enter=" + target.CanEnterNow).ToArray());
        _logger?.LogInfo($"[CustomMapRuntimeActivation] {source} detail: activeScene={snapshot.ActiveSceneName}, targets={targets}");
    }

    private static string ResolveActiveActivationState(LongLiveCustomMapRuntimeReadinessReport readiness)
    {
        foreach (var target in readiness.Targets)
        {
            if (target.MatchesActiveScene)
            {
                return ResolveActivationState(target);
            }
        }

        return string.Empty;
    }

    private static string ResolveActivationState(LongLiveCustomMapRuntimeReadinessTarget target)
    {
        if (target.IsHostBackedScene && target.RequiresCustomActivation)
        {
            return "host-proxy";
        }

        if (target.IsHostBackedScene)
        {
            return "host-backed";
        }

        if (target.NeedsCustomActivationImplementation)
        {
            return "pending-implementation";
        }

        if (target.CanEnterNow)
        {
            return "ready";
        }

        return "blocked";
    }

    private static int CountBy(IReadOnlyList<LongLiveCustomMapRuntimeReadinessTarget> targets, Func<LongLiveCustomMapRuntimeReadinessTarget, bool> predicate)
    {
        var count = 0;
        foreach (var target in targets)
        {
            if (predicate(target))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsEnabled()
    {
        return _logger is not null
            && _options?.EnableDebugLogging.Value == true
            && _options.EnableCustomMapRuntimeLogging.Value;
    }

    private static bool IsVerbose()
    {
        return IsEnabled() && _options?.EnableCustomMapRuntimeVerbose.Value == true;
    }
}
