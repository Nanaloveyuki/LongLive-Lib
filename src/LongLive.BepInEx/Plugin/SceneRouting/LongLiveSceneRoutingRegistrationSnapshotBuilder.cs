using System;
using System.Collections.Generic;
using System.Linq;
using LongLive.Mods.Maps;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveSceneRoutingRegistrationSnapshotBuilder
{
    public static LongLiveSceneRoutingRegistrationSnapshot Capture(LongLiveSceneRoutingHost host, ILongLiveSceneRoutingService sceneRouting)
    {
        if (host is null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        if (sceneRouting is null)
        {
            throw new ArgumentNullException(nameof(sceneRouting));
        }

        var routeCatalog = sceneRouting.Catalog;
        var mapOverview = host.MapOverview;
        var customMapRuntime = host.CustomMapRuntime;
        var routeSnapshot = sceneRouting.CaptureSnapshot();

        var routeCountsByModId = CountBy(routeCatalog.Routes, static route => route.OwningModId);
        var pageCountsByModId = CountBy(mapOverview.Catalog.Pages, static page => page.OwningModId);
        var regionCountsByModId = CountBy(mapOverview.Catalog.Regions, static region => region.OwningModId);
        var nodeCountsByModId = CountBy(mapOverview.Catalog.Nodes, static node => node.OwningModId);
        var routeProjectionCountsByModId = CountBy(mapOverview.Routing.Projections, static projection => projection.OwningModId);
        var runtimeSceneCountsByModId = CountBy(customMapRuntime.Catalog.Scenes, static scene => scene.OwningModId);
        var runtimeBootstrapCountsByModId = CountBy(customMapRuntime.Bootstraps.Bootstraps, static bootstrap => bootstrap.OwningModId);
        var topologyCountsByModId = CountBy(customMapRuntime.SceneLocalTopologies.Topologies, static topology => topology.OwningModId);
        var routeKindCounts = CountBy(routeCatalog.Routes, static route => route.RouteKind.ToString());

        var owningModIds = routeCountsByModId.Keys
            .Concat(pageCountsByModId.Keys)
            .Concat(regionCountsByModId.Keys)
            .Concat(nodeCountsByModId.Keys)
            .Concat(routeProjectionCountsByModId.Keys)
            .Concat(runtimeSceneCountsByModId.Keys)
            .Concat(runtimeBootstrapCountsByModId.Keys)
            .Concat(topologyCountsByModId.Keys)
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();

        return new LongLiveSceneRoutingRegistrationSnapshot
        {
            RouteCount = routeCatalog.RouteCount,
            WorldMapPageCount = mapOverview.Catalog.PageCount,
            HighlightRegionCount = mapOverview.Catalog.RegionCount,
            WorldNodeCount = mapOverview.Catalog.NodeCount,
            RouteProjectionCount = mapOverview.Routing.Projections.Count,
            CustomRuntimeSceneCount = customMapRuntime.Catalog.SceneCount,
            CustomRuntimeBootstrapCount = customMapRuntime.Bootstraps.Bootstraps.Count,
            SceneLocalTopologyCount = customMapRuntime.SceneLocalTopologies.TopologyCount,
            SceneLocalNodeCount = customMapRuntime.SceneLocalTopologies.NodeCount,
            OwningModIds = owningModIds,
            RouteCountsByModId = routeCountsByModId,
            PageCountsByModId = pageCountsByModId,
            RegionCountsByModId = regionCountsByModId,
            NodeCountsByModId = nodeCountsByModId,
            RouteProjectionCountsByModId = routeProjectionCountsByModId,
            RuntimeSceneCountsByModId = runtimeSceneCountsByModId,
            RuntimeBootstrapCountsByModId = runtimeBootstrapCountsByModId,
            TopologyCountsByModId = topologyCountsByModId,
            RouteKindCounts = routeKindCounts,
            HasActiveSceneRegistration = routeSnapshot.HasRegisteredRoute,
            ActiveSceneName = routeSnapshot.ActiveSceneName,
            ActiveSceneLogicalId = routeSnapshot.RegisteredSceneLogicalId,
            ActiveSceneOwningModId = routeSnapshot.RegisteredOwningModId,
            ActiveOverviewPageId = routeSnapshot.RegisteredOverviewPageId,
            ActiveHighlightRegionId = routeSnapshot.RegisteredHighlightRegionId,
            ActiveRouteKind = routeSnapshot.ActiveSceneKind,
        };
    }

    private static IReadOnlyDictionary<string, int> CountBy<TItem>(IEnumerable<TItem> items, Func<TItem, string> keySelector)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var key = keySelector(item) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                key = "unassigned";
            }

            if (counts.TryGetValue(key, out var current))
            {
                counts[key] = current + 1;
            }
            else
            {
                counts[key] = 1;
            }
        }

        return counts;
    }
}
