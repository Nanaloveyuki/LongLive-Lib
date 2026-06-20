using System;
using System.Collections.Generic;
using LongLive.Mods.SceneRouting;

namespace LongLive.Mods.Maps;

public sealed class LongLiveCustomMapRuntimeRegistry
{
    private readonly LongLiveCustomMapRuntimeCatalog _catalog = new LongLiveCustomMapRuntimeCatalog();
    private readonly LongLiveCustomMapRuntimeBootstrapCatalog _bootstraps = new LongLiveCustomMapRuntimeBootstrapCatalog();

    public LongLiveCustomMapRuntimeCatalog Catalog => _catalog;

    public ILongLiveCustomMapRuntimeBootstrapCatalog Bootstraps => _bootstraps;

    public void RegisterPlan(LongLiveMapRegistryPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (!plan.Validation.IsValid)
        {
            throw new InvalidOperationException("Cannot register an invalid map registry plan into custom map runtime.");
        }

        foreach (var scene in plan.Draft.Scenes)
        {
            _catalog.RegisterScene(scene);
        }

        RegisterBootstraps(plan.Draft);
    }

    private void RegisterBootstraps(LongLiveMapRegistryDraft draft)
    {
        var nodesByTargetSceneLogicalId = new Dictionary<string, LongLiveWorldNodeDescriptor>(StringComparer.Ordinal);
        var nodesByTargetSceneName = new Dictionary<string, LongLiveWorldNodeDescriptor>(StringComparer.Ordinal);

        foreach (var node in draft.Nodes)
        {
            if (!string.IsNullOrWhiteSpace(node.TargetSceneLogicalId))
            {
                nodesByTargetSceneLogicalId[node.TargetSceneLogicalId] = node;
            }

            if (!string.IsNullOrWhiteSpace(node.TargetSceneName))
            {
                nodesByTargetSceneName[node.TargetSceneName] = node;
            }
        }

        foreach (var scene in draft.Scenes)
        {
            if (!IsRuntimeCandidate(scene))
            {
                continue;
            }

            var entryNode = ResolveEntryNode(scene, nodesByTargetSceneLogicalId, nodesByTargetSceneName);
            var returnScene = ResolveReturnScene(scene, draft.Scenes);
            var routeKind = LongLiveSceneRoutingMapper.ToRouteKind(scene.MapKind);

            _bootstraps.Register(new LongLiveCustomMapRuntimeBootstrapDescriptor
            {
                SceneLogicalId = scene.LogicalId,
                SceneName = scene.SceneName,
                OwningModId = scene.OwningModId,
                OverviewPageId = scene.OverviewPageId,
                HighlightRegionId = scene.HighlightRegionId,
                DisplayName = !string.IsNullOrWhiteSpace(scene.DisplayName) ? scene.DisplayName : scene.EventName,
                MapKind = scene.MapKind,
                RouteKind = routeKind,
                HostMapType = scene.HostMapType,
                EntryNodeLogicalId = entryNode?.LogicalId ?? string.Empty,
                PreferredEntryIndex = ResolvePreferredEntryIndex(scene, entryNode, routeKind),
                ReturnSceneLogicalId = returnScene?.LogicalId ?? scene.OutsideSceneLogicalId,
                ReturnSceneName = returnScene?.SceneName ?? scene.OutsideSceneName,
                PreferredReturnEntryIndex = returnScene?.HostOutsideScenePos,
                AssetBundleId = scene.AssetBundleId,
            });
        }
    }

    private static bool IsRuntimeCandidate(LongLiveSceneDescriptor scene)
    {
        return !string.IsNullOrWhiteSpace(scene.SceneName)
            && scene.MapKind != LongLiveMapKind.Unknown
            && scene.MapKind != LongLiveMapKind.World;
    }

    private static LongLiveWorldNodeDescriptor? ResolveEntryNode(
        LongLiveSceneDescriptor scene,
        IReadOnlyDictionary<string, LongLiveWorldNodeDescriptor> nodesByTargetSceneLogicalId,
        IReadOnlyDictionary<string, LongLiveWorldNodeDescriptor> nodesByTargetSceneName)
    {
        if (!string.IsNullOrWhiteSpace(scene.LogicalId) && nodesByTargetSceneLogicalId.TryGetValue(scene.LogicalId, out var byLogicalId))
        {
            return byLogicalId;
        }

        if (!string.IsNullOrWhiteSpace(scene.SceneName) && nodesByTargetSceneName.TryGetValue(scene.SceneName, out var bySceneName))
        {
            return bySceneName;
        }

        return null;
    }

    private static LongLiveSceneDescriptor? ResolveReturnScene(LongLiveSceneDescriptor scene, IReadOnlyList<LongLiveSceneDescriptor> scenes)
    {
        if (string.IsNullOrWhiteSpace(scene.OutsideSceneLogicalId) && string.IsNullOrWhiteSpace(scene.OutsideSceneName))
        {
            return null;
        }

        foreach (var candidate in scenes)
        {
            if (!string.IsNullOrWhiteSpace(scene.OutsideSceneLogicalId) && string.Equals(candidate.LogicalId, scene.OutsideSceneLogicalId, StringComparison.Ordinal))
            {
                return candidate;
            }

            if (!string.IsNullOrWhiteSpace(scene.OutsideSceneName) && string.Equals(candidate.SceneName, scene.OutsideSceneName, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    private static int? ResolvePreferredEntryIndex(LongLiveSceneDescriptor scene, LongLiveWorldNodeDescriptor? entryNode, LongLiveSceneRouteKind routeKind)
    {
        if (scene.HostOutsideScenePos.HasValue)
        {
            return scene.HostOutsideScenePos.Value;
        }

        if (routeKind == LongLiveSceneRouteKind.WorldMap && entryNode?.HostNodeIndex is not null)
        {
            return entryNode.HostNodeIndex.Value;
        }

        return null;
    }
}
