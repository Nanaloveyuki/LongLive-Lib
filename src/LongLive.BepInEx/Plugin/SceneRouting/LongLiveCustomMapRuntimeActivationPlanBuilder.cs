using System;
using System.Collections.Generic;
using System.Linq;
using LongLive.Mods.Maps;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCustomMapRuntimeActivationPlanBuilder
{
    private const string HostBaseGameModId = "host.base-game";

    public static LongLiveCustomMapRuntimeActivationPlan Build(ILongLiveCustomMapRuntimeFeature feature)
    {
        if (feature is null)
        {
            throw new ArgumentNullException(nameof(feature));
        }

        var routingSnapshot = LongLivePluginContext.SceneRouting.CaptureSnapshot();
        var topologyCatalog = feature.SceneLocalTopologies;
        var targets = new List<LongLiveCustomMapRuntimeActivationTarget>();
        foreach (var bootstrap in feature.Bootstraps.Bootstraps.OrderBy(static bootstrap => bootstrap.OwningModId, StringComparer.Ordinal).ThenBy(static bootstrap => bootstrap.SceneLogicalId, StringComparer.Ordinal))
        {
            var topologyNodeCount = ResolveTopologyNodeCount(topologyCatalog, bootstrap.SceneLogicalId, bootstrap.SceneName);
            var matchesActiveScene = (!string.IsNullOrWhiteSpace(routingSnapshot.RegisteredSceneLogicalId) && string.Equals(bootstrap.SceneLogicalId, routingSnapshot.RegisteredSceneLogicalId, StringComparison.Ordinal))
                || (!string.IsNullOrWhiteSpace(routingSnapshot.ActiveSceneName) && string.Equals(bootstrap.SceneName, routingSnapshot.ActiveSceneName, StringComparison.Ordinal));

            targets.Add(new LongLiveCustomMapRuntimeActivationTarget
            {
                SceneLogicalId = bootstrap.SceneLogicalId,
                SceneName = bootstrap.SceneName,
                OwningModId = bootstrap.OwningModId,
                DisplayName = bootstrap.DisplayName,
                OverviewPageId = bootstrap.OverviewPageId,
                HighlightRegionId = bootstrap.HighlightRegionId,
                EntryNodeLogicalId = bootstrap.EntryNodeLogicalId,
                ReturnSceneLogicalId = bootstrap.ReturnSceneLogicalId,
                ReturnSceneName = bootstrap.ReturnSceneName,
                PreferredEntryIndex = bootstrap.PreferredEntryIndex,
                PreferredReturnEntryIndex = bootstrap.PreferredReturnEntryIndex,
                AssetBundleId = bootstrap.AssetBundleId,
                RouteKind = bootstrap.RouteKind,
                MatchesActiveScene = matchesActiveScene,
                RequiresCustomActivation = RequiresCustomActivation(bootstrap, topologyNodeCount),
                SceneLocalTopologyNodeCount = topologyNodeCount,
                EntryAddress = bootstrap.CreateEntryAddress(),
                ReturnAddress = bootstrap.CreateReturnAddress(),
            });
        }

        return new LongLiveCustomMapRuntimeActivationPlan
        {
            ActiveSceneName = routingSnapshot.ActiveSceneName,
            ActiveSceneLogicalId = routingSnapshot.RegisteredSceneLogicalId,
            ActiveOwningModId = routingSnapshot.RegisteredOwningModId,
            HasActiveTarget = targets.Any(static target => target.MatchesActiveScene),
            HasActiveBootstrapMatch = targets.Any(static target => target.MatchesActiveScene && target.RequiresCustomActivation),
            RuntimeSceneCount = feature.Catalog.SceneCount,
            BootstrapCount = feature.Bootstraps.Bootstraps.Count,
            ActivationTargetCount = targets.Count,
            ExternalActivationTargetCount = targets.Count(static target => target.RequiresCustomActivation),
            TopologyCount = topologyCatalog.TopologyCount,
            TopologyNodeCount = topologyCatalog.NodeCount,
            Targets = targets,
        };
    }

    private static int ResolveTopologyNodeCount(ILongLiveSceneLocalTopologyCatalog topologyCatalog, string sceneLogicalId, string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneLogicalId) && topologyCatalog.TryGetTopologyBySceneLogicalId(sceneLogicalId, out var byLogicalId) && byLogicalId is not null)
        {
            return topologyCatalog.GetNodesForTopology(byLogicalId.LogicalId).Count;
        }

        if (!string.IsNullOrWhiteSpace(sceneName) && topologyCatalog.TryGetTopologyBySceneName(sceneName, out var bySceneName) && bySceneName is not null)
        {
            return topologyCatalog.GetNodesForTopology(bySceneName.LogicalId).Count;
        }

        return 0;
    }

    private static bool RequiresCustomActivation(LongLiveCustomMapRuntimeBootstrapDescriptor bootstrap, int topologyNodeCount)
    {
        if (!string.Equals(bootstrap.OwningModId, HostBaseGameModId, StringComparison.Ordinal))
        {
            return true;
        }

        if (topologyNodeCount > 0)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(bootstrap.AssetBundleId);
    }
}
