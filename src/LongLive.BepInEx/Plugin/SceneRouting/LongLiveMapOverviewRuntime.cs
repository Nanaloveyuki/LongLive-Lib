using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using LongLive.Mods.Maps;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapOverviewRuntime
{
    private static ManualLogSource? _logger;
    private static LongLiveHostOptions? _options;

    public static void Initialize(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public static LongLiveMapOverviewRuntimeSnapshot CaptureSnapshot(int sampleLimit = 8)
    {
        var feature = LongLivePluginContext.MapOverview;
        var installPlan = LongLiveMapOverviewInstallPlanBuilder.Build(feature);
        var sceneSnapshot = LongLivePluginContext.SceneRouting.CaptureSnapshot();
        var snapshot = new LongLiveMapOverviewRuntimeSnapshot
        {
            ActiveSceneName = sceneSnapshot.ActiveSceneName,
            TotalPageCount = feature.Catalog.PageCount,
            TotalRegionCount = feature.Catalog.RegionCount,
            TotalNodeCount = feature.Catalog.NodeCount,
            TotalProjectionCount = feature.Routing.Projections.Count,
            ExternalPageTargetCount = installPlan.ExternalPageTargetCount,
            ExternalProjectionCount = installPlan.ExternalProjectionCount,
            RegisteredModIds = BuildRegisteredModIds(feature),
            HostBindingRuntime = LongLiveMapOverviewHostBindingRuntime.CaptureSnapshot(),
            CustomPageRuntime = LongLiveMapOverviewCustomPageRuntime.CaptureSnapshot(),
            InstallPlan = installPlan,
        };

        if (!sceneSnapshot.HasRegisteredRoute)
        {
            return snapshot;
        }

        snapshot.ActiveSceneLogicalId = sceneSnapshot.RegisteredSceneLogicalId;
        snapshot.ActiveOwningModId = sceneSnapshot.RegisteredOwningModId;
        snapshot.ActivePageId = sceneSnapshot.RegisteredOverviewPageId;
        snapshot.ActiveRegionId = sceneSnapshot.RegisteredHighlightRegionId;

        if (!string.IsNullOrWhiteSpace(sceneSnapshot.RegisteredSceneLogicalId) && feature.Routing.TryGetBySceneLogicalId(sceneSnapshot.RegisteredSceneLogicalId, out var projection) && projection is not null)
        {
            snapshot.HasActiveProjection = true;
            snapshot.ActivePageId = string.IsNullOrWhiteSpace(snapshot.ActivePageId) ? projection.PageId : snapshot.ActivePageId;
            snapshot.ActiveRegionId = string.IsNullOrWhiteSpace(snapshot.ActiveRegionId) ? projection.RegionId : snapshot.ActiveRegionId;
            snapshot.ActiveOwningModId = string.IsNullOrWhiteSpace(snapshot.ActiveOwningModId) ? projection.OwningModId : snapshot.ActiveOwningModId;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.ActivePageId))
        {
            snapshot.ActivePageNodeNames = SampleNodeNames(feature.Catalog.GetNodesForPage(snapshot.ActivePageId), sampleLimit);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.ActiveRegionId))
        {
            snapshot.ActiveRegionNodeNames = SampleNodeNames(feature.Catalog.GetNodesForRegion(snapshot.ActiveRegionId), sampleLimit);
        }

        return snapshot;
    }

    public static void LogInstallerSummary()
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[MapOverviewRuntime] ready: pages={snapshot.TotalPageCount}, regions={snapshot.TotalRegionCount}, nodes={snapshot.TotalNodeCount}, projections={snapshot.TotalProjectionCount}, externalPages={snapshot.ExternalPageTargetCount}, externalProjections={snapshot.ExternalProjectionCount}, activeScene={snapshot.ActiveSceneName}, activePage={(string.IsNullOrWhiteSpace(snapshot.ActivePageId) ? "n/a" : snapshot.ActivePageId)}");
        LogVerboseSnapshot(snapshot, "installer");
    }

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[MapOverviewRuntime] sceneLoaded: scene={scene.name}, mode={mode}, activeProjection={snapshot.HasActiveProjection}, activeSceneLogicalId={snapshot.ActiveSceneLogicalId}, activePage={(string.IsNullOrWhiteSpace(snapshot.ActivePageId) ? "n/a" : snapshot.ActivePageId)}, activeRegion={(string.IsNullOrWhiteSpace(snapshot.ActiveRegionId) ? "n/a" : snapshot.ActiveRegionId)}, externalPages={snapshot.ExternalPageTargetCount}");
        LogVerboseSnapshot(snapshot, "sceneLoaded");
    }

    private static void LogVerboseSnapshot(LongLiveMapOverviewRuntimeSnapshot snapshot, string source)
    {
        if (!IsVerbose())
        {
            return;
        }

        var mods = snapshot.RegisteredModIds.Count == 0 ? "n/a" : string.Join(", ", snapshot.RegisteredModIds.ToArray());
        var pageNodes = snapshot.ActivePageNodeNames.Count == 0 ? "n/a" : string.Join(", ", snapshot.ActivePageNodeNames.ToArray());
        var regionNodes = snapshot.ActiveRegionNodeNames.Count == 0 ? "n/a" : string.Join(", ", snapshot.ActiveRegionNodeNames.ToArray());
        var installTargets = snapshot.InstallPlan.PageTargets.Count == 0
            ? "n/a"
            : string.Join(" | ", snapshot.InstallPlan.PageTargets.Take(6).Select(static target => target.PageId + ":regions=" + target.RegionCount + ",nodes=" + target.NodeCount + ",inject=" + target.RequiresHostInjection).ToArray());
        var customPageSummary = JoinCustomPageSummary(snapshot.CustomPageRuntime);
        _logger?.LogInfo($"[MapOverviewRuntime] {source} detail: activeScene={snapshot.ActiveSceneName}, activeOwningMod={snapshot.ActiveOwningModId}, registeredMods={mods}, pageNodes={pageNodes}, regionNodes={regionNodes}, installTargets={installTargets}, bindableTargets={snapshot.HostBindingRuntime.BindableTargetCount}, customPages={customPageSummary}");
    }

    private static string JoinCustomPageSummary(LongLiveMapOverviewCustomPageRuntimeSnapshot snapshot)
    {
        if (snapshot.CustomPageTargetCount == 0)
        {
            return "n/a";
        }

        var mountedPages = snapshot.MountedPageIds.Count == 0
            ? "n/a"
            : string.Join(",", snapshot.MountedPageIds.ToArray());
        var activePage = string.IsNullOrWhiteSpace(snapshot.ActivePageId) ? "n/a" : snapshot.ActivePageId;
        return $"targets={snapshot.CustomPageTargetCount}, tabs={snapshot.MountedTabButtonCount}, roots={snapshot.MountedPageRootCount}, active={activePage}, mounted={mountedPages}";
    }

    private static List<string> BuildRegisteredModIds(ILongLiveMapOverviewFeature feature)
    {
        return feature.Catalog.Pages
            .Select(static page => page.OwningModId)
            .Concat(feature.Catalog.Regions.Select(static region => region.OwningModId))
            .Concat(feature.Catalog.Nodes.Select(static node => node.OwningModId))
            .Concat(feature.Routing.Projections.Select(static projection => projection.OwningModId))
            .Where(static modId => !string.IsNullOrWhiteSpace(modId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static modId => modId, StringComparer.Ordinal)
            .ToList();
    }

    private static List<string> SampleNodeNames(IReadOnlyList<LongLiveWorldNodeDescriptor> nodes, int sampleLimit)
    {
        return nodes
            .Where(static node => !string.IsNullOrWhiteSpace(node.DisplayName))
            .Take(sampleLimit)
            .Select(static node => node.DisplayName)
            .ToList();
    }

    private static bool IsEnabled()
    {
        return _logger is not null
            && _options?.EnableDebugLogging.Value == true
            && _options.EnableMapOverviewRuntimeLogging.Value;
    }

    private static bool IsVerbose()
    {
        return IsEnabled() && _options?.EnableMapOverviewRuntimeVerbose.Value == true;
    }
}
