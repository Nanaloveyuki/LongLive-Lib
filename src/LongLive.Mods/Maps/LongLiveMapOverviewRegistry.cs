using System;
using System.Collections.Generic;
using LongLive.Mods.SceneRouting;

namespace LongLive.Mods.Maps;

public sealed class LongLiveMapOverviewRegistry
{
    private readonly LongLiveMapOverviewCatalog _catalog = new LongLiveMapOverviewCatalog();
    private readonly LongLiveMapOverviewRoutingProjectionCatalog _routing = new LongLiveMapOverviewRoutingProjectionCatalog();

    public LongLiveMapOverviewCatalog Catalog => _catalog;

    public ILongLiveMapOverviewRoutingProjectionCatalog Routing => _routing;

    public void RegisterPlan(LongLiveMapRegistryPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (!plan.Validation.IsValid)
        {
            throw new InvalidOperationException("Cannot register an invalid map registry plan into map overview.");
        }

        foreach (var page in plan.Draft.Pages)
        {
            _catalog.RegisterPage(page);
        }

        foreach (var region in plan.Draft.HighlightRegions)
        {
            _catalog.RegisterRegion(region);
        }

        foreach (var node in plan.Draft.Nodes)
        {
            _catalog.RegisterNode(node);
        }

        RegisterRouteProjections(plan.Draft);
    }

    private void RegisterRouteProjections(LongLiveMapRegistryDraft draft)
    {
        var scenesByLogicalId = new Dictionary<string, LongLiveSceneDescriptor>(StringComparer.Ordinal);
        var scenesByName = new Dictionary<string, LongLiveSceneDescriptor>(StringComparer.Ordinal);

        foreach (var scene in draft.Scenes)
        {
            if (!string.IsNullOrWhiteSpace(scene.LogicalId))
            {
                scenesByLogicalId[scene.LogicalId] = scene;
            }

            if (!string.IsNullOrWhiteSpace(scene.SceneName))
            {
                scenesByName[scene.SceneName] = scene;
            }
        }

        foreach (var node in draft.Nodes)
        {
            if (!TryResolveTargetScene(node, scenesByLogicalId, scenesByName, out var scene) || scene is null)
            {
                continue;
            }

            _routing.Register(new LongLiveMapOverviewRouteProjection
            {
                OwningModId = scene.OwningModId,
                PageId = node.PageId,
                RegionId = scene.HighlightRegionId,
                NodeLogicalId = node.LogicalId,
                NodeDisplayName = node.DisplayName,
                HostNodeIndex = node.HostNodeIndex,
                SceneLogicalId = scene.LogicalId,
                SceneName = scene.SceneName,
                SceneDisplayName = !string.IsNullOrWhiteSpace(scene.DisplayName) ? scene.DisplayName : node.DisplayName,
                RouteKind = LongLiveSceneRoutingMapper.ToRouteKind(scene.MapKind),
                PreferredEntryIndex = ResolvePreferredEntryIndex(node, scene),
                AccessStaticValueId = node.AccessStaticValueId,
                HideOnLock = node.HideOnLock,
                AccessRuleSummary = node.AccessRuleSummary,
            });
        }
    }

    private static bool TryResolveTargetScene(
        LongLiveWorldNodeDescriptor node,
        IReadOnlyDictionary<string, LongLiveSceneDescriptor> scenesByLogicalId,
        IReadOnlyDictionary<string, LongLiveSceneDescriptor> scenesByName,
        out LongLiveSceneDescriptor? scene)
    {
        if (!string.IsNullOrWhiteSpace(node.TargetSceneLogicalId) && scenesByLogicalId.TryGetValue(node.TargetSceneLogicalId, out scene))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(node.TargetSceneName) && scenesByName.TryGetValue(node.TargetSceneName, out scene))
        {
            return true;
        }

        scene = null;
        return false;
    }

    private static int? ResolvePreferredEntryIndex(LongLiveWorldNodeDescriptor node, LongLiveSceneDescriptor scene)
    {
        var routeKind = LongLiveSceneRoutingMapper.ToRouteKind(scene.MapKind);

        if (scene.HostOutsideScenePos.HasValue)
        {
            return scene.HostOutsideScenePos.Value;
        }

        if (routeKind == LongLiveSceneRouteKind.WorldMap && node.HostNodeIndex.HasValue)
        {
            return node.HostNodeIndex.Value;
        }

        return null;
    }
}
