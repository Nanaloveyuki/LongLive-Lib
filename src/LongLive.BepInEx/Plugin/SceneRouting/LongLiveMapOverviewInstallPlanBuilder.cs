using System;
using System.Collections.Generic;
using System.Linq;
using LongLive.Mods.Maps;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapOverviewInstallPlanBuilder
{
    private const string HostBaseGameModId = "host.base-game";

    public static LongLiveMapOverviewInstallPlan Build(ILongLiveMapOverviewFeature feature)
    {
        if (feature is null)
        {
            throw new ArgumentNullException(nameof(feature));
        }

        var routingSnapshot = LongLivePluginContext.SceneRouting.CaptureSnapshot();
        var pageTargets = new List<LongLiveMapOverviewPageInstallTarget>();
        foreach (var page in feature.Catalog.Pages.OrderBy(static page => page.OrderHint ?? int.MaxValue).ThenBy(static page => page.LogicalId, StringComparer.Ordinal))
        {
            var regions = feature.Catalog.GetRegionsForPage(page.LogicalId);
            var nodes = feature.Catalog.GetNodesForPage(page.LogicalId);
            var projections = feature.Routing.GetByPageId(page.LogicalId);
            pageTargets.Add(new LongLiveMapOverviewPageInstallTarget
            {
                PageId = page.LogicalId,
                OwningModId = page.OwningModId,
                DisplayName = page.DisplayName,
                BackgroundAssetId = page.BackgroundAssetId,
                TabIconAssetId = page.TabIconAssetId,
                OrderHint = page.OrderHint,
                RegionCount = regions.Count,
                NodeCount = nodes.Count,
                ProjectionCount = projections.Count,
                ContainsActiveProjection = string.Equals(page.LogicalId, routingSnapshot.RegisteredOverviewPageId, StringComparison.Ordinal),
                RequiresHostInjection = RequiresHostInjection(page, projections),
                RegionIds = regions.Select(static region => region.LogicalId).ToArray(),
                NodeIds = nodes.Select(static node => node.LogicalId).ToArray(),
                ProjectionSceneLogicalIds = projections.Select(static projection => projection.SceneLogicalId).Where(static logicalId => !string.IsNullOrWhiteSpace(logicalId)).Distinct(StringComparer.Ordinal).ToArray(),
            });
        }

        return new LongLiveMapOverviewInstallPlan
        {
            ActiveSceneName = routingSnapshot.ActiveSceneName,
            ActiveSceneLogicalId = routingSnapshot.RegisteredSceneLogicalId,
            ActiveOwningModId = routingSnapshot.RegisteredOwningModId,
            HasActivePageTarget = pageTargets.Any(target => target.ContainsActiveProjection),
            ActivePageId = routingSnapshot.RegisteredOverviewPageId,
            ActiveRegionId = routingSnapshot.RegisteredHighlightRegionId,
            PageTargetCount = feature.Catalog.PageCount,
            RegionTargetCount = feature.Catalog.RegionCount,
            NodeTargetCount = feature.Catalog.NodeCount,
            ProjectionCount = feature.Routing.Projections.Count,
            ExternalPageTargetCount = pageTargets.Count(static target => target.RequiresHostInjection),
            ExternalProjectionCount = feature.Routing.Projections.Count(static projection => !string.Equals(projection.OwningModId, HostBaseGameModId, StringComparison.Ordinal)),
            PageTargets = pageTargets,
        };
    }

    private static bool RequiresHostInjection(LongLiveWorldMapPageDescriptor page, IReadOnlyList<LongLiveMapOverviewRouteProjection> projections)
    {
        if (!string.Equals(page.OwningModId, HostBaseGameModId, StringComparison.Ordinal))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(page.BackgroundAssetId) || !string.IsNullOrWhiteSpace(page.TabIconAssetId))
        {
            return true;
        }

        return projections.Any(static projection => !string.Equals(projection.OwningModId, HostBaseGameModId, StringComparison.Ordinal));
    }
}
