using System;
using System.Collections.Generic;
using JSONClass;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCustomMapRuntimeReadinessEvaluator
{
    public static LongLiveCustomMapRuntimeReadinessReport Evaluate(LongLiveCustomMapRuntimeActivationPlan activationPlan)
    {
        if (activationPlan is null)
        {
            throw new ArgumentNullException(nameof(activationPlan));
        }

        var targets = new List<LongLiveCustomMapRuntimeReadinessTarget>();
        LongLiveCustomMapRuntimeReadinessTarget? activeTarget = null;
        foreach (var target in activationPlan.Targets)
        {
            var readiness = EvaluateTarget(target);
            targets.Add(readiness);
            if (readiness.MatchesActiveScene && activeTarget is null)
            {
                activeTarget = readiness;
            }
        }

        return new LongLiveCustomMapRuntimeReadinessReport
        {
            ActiveSceneName = activationPlan.ActiveSceneName,
            ActiveSceneLogicalId = activationPlan.ActiveSceneLogicalId,
            TargetCount = targets.Count,
            ReadyTargetCount = CountBy(targets, static target => target.CanEnterNow),
            BlockedTargetCount = CountBy(targets, static target => !target.CanEnterNow),
            HostBackedTargetCount = CountBy(targets, static target => target.IsHostBackedScene),
            ActivationPendingTargetCount = CountBy(targets, static target => target.NeedsCustomActivationImplementation),
            HasActiveTarget = activeTarget is not null,
            ActiveTargetSceneLogicalId = activeTarget?.SceneLogicalId ?? string.Empty,
            ActiveStatusCode = activeTarget?.StatusCode ?? string.Empty,
            ActiveDetail = activeTarget?.Detail ?? string.Empty,
            Targets = targets,
        };
    }

    public static bool TryEvaluateForAddress(LongLiveSceneAddress address, LongLiveSceneRouteResolution resolution, out LongLiveCustomMapRuntimeReadinessTarget? readiness)
    {
        if (address is null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        if (resolution is null)
        {
            throw new ArgumentNullException(nameof(resolution));
        }

        var activationPlan = LongLivePluginContext.GetCustomMapRuntimeActivationPlan();
        var target = FindTarget(
            activationPlan,
            address.LogicalSceneId ?? string.Empty,
            resolution.Descriptor?.LogicalId ?? string.Empty,
            address.SceneName ?? string.Empty,
            resolution.Descriptor?.SceneName ?? string.Empty);
        if (target is null)
        {
            readiness = null;
            return false;
        }

        readiness = EvaluateTarget(target);
        return true;
    }

    public static LongLiveCustomMapRuntimeReadinessTarget EvaluateTarget(LongLiveCustomMapRuntimeActivationTarget target)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var routing = LongLivePluginContext.SceneRouting;
        var entryResolution = routing.Resolve(target.EntryAddress);
        var returnResolution = routing.Resolve(target.ReturnAddress);
        var entryResolvable = entryResolution.RouteKind != LongLiveSceneRouteKind.Unknown;
        var returnRequired = !string.IsNullOrWhiteSpace(target.ReturnAddress.SceneName) || !string.IsNullOrWhiteSpace(target.ReturnAddress.LogicalSceneId);
        var returnResolvable = !returnRequired || returnResolution.RouteKind != LongLiveSceneRouteKind.Unknown;
        var entryIndexSane = !target.PreferredEntryIndex.HasValue || target.PreferredEntryIndex.Value >= 0;
        var returnIndexSane = !target.PreferredReturnEntryIndex.HasValue || target.PreferredReturnEntryIndex.Value >= 0;
        var isHostBackedScene = IsHostBackedScene(target.SceneName);
        var needsCustomActivationImplementation = target.RequiresCustomActivation && !isHostBackedScene;

        string statusCode;
        bool canEnterNow;
        if (!entryResolvable || !returnResolvable)
        {
            statusCode = "route-unresolved";
            canEnterNow = false;
        }
        else if (!entryIndexSane || !returnIndexSane)
        {
            statusCode = "entry-index-invalid";
            canEnterNow = false;
        }
        else if (isHostBackedScene && target.RequiresCustomActivation)
        {
            statusCode = "host-backed-proxy";
            canEnterNow = true;
        }
        else if (isHostBackedScene)
        {
            statusCode = "host-backed-ready";
            canEnterNow = true;
        }
        else if (target.RequiresCustomActivation)
        {
            statusCode = "custom-activation-required";
            canEnterNow = false;
        }
        else
        {
            statusCode = "host-scene-missing";
            canEnterNow = false;
        }

        return new LongLiveCustomMapRuntimeReadinessTarget
        {
            SceneLogicalId = target.SceneLogicalId,
            SceneName = target.SceneName,
            OwningModId = target.OwningModId,
            DisplayName = target.DisplayName,
            EntryNodeLogicalId = target.EntryNodeLogicalId,
            EntrySceneName = target.EntryAddress.SceneName,
            ReturnSceneName = target.ReturnAddress.SceneName,
            AssetBundleId = target.AssetBundleId,
            MatchesActiveScene = target.MatchesActiveScene,
            RequiresCustomActivation = target.RequiresCustomActivation,
            NeedsCustomActivationImplementation = needsCustomActivationImplementation,
            IsHostBackedScene = isHostBackedScene,
            EntryRouteResolvable = entryResolvable,
            ReturnRouteResolvable = returnResolvable,
            ReturnRouteRequired = returnRequired,
            PreferredEntryIndexSane = entryIndexSane,
            PreferredReturnEntryIndexSane = returnIndexSane,
            EntryRouteKind = entryResolution.RouteKind.ToString(),
            ReturnRouteKind = returnResolution.RouteKind.ToString(),
            TopologyNodeCount = target.SceneLocalTopologyNodeCount,
            CanEnterNow = canEnterNow,
            StatusCode = statusCode,
            Detail = BuildDetail(statusCode, target, entryResolution.RouteKind, returnResolution.RouteKind, returnRequired, entryIndexSane, returnIndexSane, isHostBackedScene),
        };
    }

    private static LongLiveCustomMapRuntimeActivationTarget? FindTarget(LongLiveCustomMapRuntimeActivationPlan activationPlan, string logicalSceneId, string registeredLogicalSceneId, string sceneName, string registeredSceneName)
    {
        foreach (var target in activationPlan.Targets)
        {
            if (!string.IsNullOrWhiteSpace(logicalSceneId) && string.Equals(target.SceneLogicalId, logicalSceneId, StringComparison.Ordinal))
            {
                return target;
            }

            if (!string.IsNullOrWhiteSpace(registeredLogicalSceneId) && string.Equals(target.SceneLogicalId, registeredLogicalSceneId, StringComparison.Ordinal))
            {
                return target;
            }

            if (!string.IsNullOrWhiteSpace(sceneName) && string.Equals(target.SceneName, sceneName, StringComparison.Ordinal))
            {
                return target;
            }

            if (!string.IsNullOrWhiteSpace(registeredSceneName) && string.Equals(target.SceneName, registeredSceneName, StringComparison.Ordinal))
            {
                return target;
            }
        }

        return null;
    }

    private static string BuildDetail(
        string statusCode,
        LongLiveCustomMapRuntimeActivationTarget target,
        LongLiveSceneRouteKind entryRouteKind,
        LongLiveSceneRouteKind returnRouteKind,
        bool returnRequired,
        bool entryIndexSane,
        bool returnIndexSane,
        bool isHostBackedScene)
    {
        switch (statusCode)
        {
            case "route-unresolved":
                return $"The runtime route graph is still incomplete. entryRouteKind={entryRouteKind}, returnRouteKind={returnRouteKind}, entryScene={target.EntryAddress.SceneName}, returnScene={target.ReturnAddress.SceneName}, returnRequired={returnRequired}.";
            case "entry-index-invalid":
                return $"The runtime route graph resolved, but at least one preferred entry index is invalid. preferredEntryIndex={(target.PreferredEntryIndex.HasValue ? target.PreferredEntryIndex.Value.ToString() : "n/a")}, preferredReturnIndex={(target.PreferredReturnEntryIndex.HasValue ? target.PreferredReturnEntryIndex.Value.ToString() : "n/a")}, entryIndexSane={entryIndexSane}, returnIndexSane={returnIndexSane}.";
            case "host-backed-proxy":
                return $"The runtime target currently reuses a host-backed scene and can be entered now, but it is still acting as a proxy shell for future custom activation. sceneName={target.SceneName}, mod={target.OwningModId}, assetBundle={(string.IsNullOrWhiteSpace(target.AssetBundleId) ? "n/a" : target.AssetBundleId)}, topologyNodes={target.SceneLocalTopologyNodeCount}.";
            case "host-backed-ready":
                return $"The runtime target is already backed by a host scene and can be entered immediately. sceneName={target.SceneName}, mod={target.OwningModId}, topologyNodes={target.SceneLocalTopologyNodeCount}.";
            case "custom-activation-required":
                return $"The runtime target is registered, but the host does not expose a real scene for it yet. LongLive still needs to install custom activation for sceneLogicalId={target.SceneLogicalId}, sceneName={target.SceneName}, mod={target.OwningModId}, assetBundle={(string.IsNullOrWhiteSpace(target.AssetBundleId) ? "n/a" : target.AssetBundleId)}, topologyNodes={target.SceneLocalTopologyNodeCount}.";
            default:
                return $"The runtime target has no host-backed scene metadata and no custom activation path yet. sceneLogicalId={target.SceneLogicalId}, sceneName={target.SceneName}, mod={target.OwningModId}, hostBacked={isHostBackedScene}.";
        }
    }

    private static bool IsHostBackedScene(string sceneName)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(sceneName)
                && SceneNameJsonData.DataDict.TryGetValue(sceneName, out var metadata)
                && metadata is not null;
        }
        catch
        {
            return false;
        }
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
}
