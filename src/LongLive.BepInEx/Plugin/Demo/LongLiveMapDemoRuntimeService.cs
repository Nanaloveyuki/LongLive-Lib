using System;
using System.Linq;
using BepInEx.Logging;
using LongLive.Mods.Maps;
using LongLive.Mods.SceneRouting;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

internal sealed class LongLiveMapDemoRuntimeService
{
    private readonly ManualLogSource _logger;
    private readonly NextRuntimeFacade _runtime;

    public LongLiveMapDemoRuntimeService(ManualLogSource logger, NextRuntimeFacade runtime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public void PublishRegistrationState(LongLiveMapRegistryPlan plan, LongLiveSceneLocalTopologyBatch topology)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (topology is null)
        {
            throw new ArgumentNullException(nameof(topology));
        }

        _runtime.SetInt(LongLiveMapDemoStateKeys.Registered, 1);
        _runtime.SetString(LongLiveMapDemoStateKeys.PlanSummary, $"pages={plan.Draft.Pages.Count}, regions={plan.Draft.HighlightRegions.Count}, scenes={plan.Draft.Scenes.Count}, nodes={plan.Draft.Nodes.Count}");
        _runtime.SetString(LongLiveMapDemoStateKeys.TopologySummary, $"topologies={topology.Topologies.Count}, nodes={topology.Nodes.Count}");

        var resolution = LongLivePluginContext.SceneRouting.Resolve(CreateDemoAddress());

        _runtime.SetString(LongLiveMapDemoStateKeys.RouteKind, resolution.RouteKind.ToString());

        var nodeSummary = string.Join(",", LongLivePluginContext.MapOverview.Routing.Projections
            .Where(static node => string.Equals(node.OwningModId, LongLiveMapDemoConstants.OwningModId, StringComparison.Ordinal))
            .Select(static node => node.NodeLogicalId)
            .Distinct(StringComparer.Ordinal)
            .ToArray());
        _runtime.SetString(LongLiveMapDemoStateKeys.NodeSummary, nodeSummary);
        _runtime.SetString(LongLiveMapDemoStateKeys.ResolveStatus, BuildResolveSummary(resolution));
    }

    public LongLiveMapDemoRuntimeSnapshot CaptureSnapshot(LongLiveHostOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var registration = LongLivePluginContext.GetSceneRoutingRegistrationSnapshot();
        var mapOverviewFeature = LongLivePluginContext.MapOverview;
        var customRuntimeFeature = LongLivePluginContext.CustomMapRuntime;
        var mapOverview = LongLivePluginContext.GetMapOverviewRuntimeSnapshot();
        var customPageRuntime = LongLivePluginContext.GetMapOverviewCustomPageRuntimeSnapshot(int.MaxValue);
        var customRuntime = LongLivePluginContext.GetCustomMapRuntimeStateSnapshot();
        var allocation = LongLivePluginContext.GetMapOverviewShellAllocationRuntimeSnapshot(int.MaxValue);
        var reservation = LongLivePluginContext.GetMapOverviewShellReservationRuntimeSnapshot(int.MaxValue);
        var topology = LongLivePluginContext.GetSceneLocalTopologyRuntimeSnapshot(int.MaxValue);
        var pageCount = mapOverviewFeature.Catalog.GetPagesForMod(LongLiveMapDemoConstants.OwningModId).Count;
        var regionCount = mapOverviewFeature.Catalog.GetRegionsForMod(LongLiveMapDemoConstants.OwningModId).Count;
        var worldNodeCount = mapOverviewFeature.Catalog.GetNodesForMod(LongLiveMapDemoConstants.OwningModId).Count;
        var runtimeSceneCount = customRuntimeFeature.Catalog.GetScenesForMod(LongLiveMapDemoConstants.OwningModId).Count;
        var topologyCount = customRuntimeFeature.SceneLocalTopologies.GetTopologiesForMod(LongLiveMapDemoConstants.OwningModId).Count;
        var topologyNodeCount = 0;
        if (topologyCount > 0)
        {
            topologyNodeCount += customRuntimeFeature.SceneLocalTopologies.GetNodesForTopology(LongLiveMapDemoConstants.TopologyId).Count;
            topologyNodeCount += customRuntimeFeature.SceneLocalTopologies.GetNodesForTopology(LongLiveMapDemoConstants.SecondTopologyId).Count;
        }

        var pageNodes = mapOverviewFeature.Catalog.GetNodesForMod(LongLiveMapDemoConstants.OwningModId);
        var nodeSummary = string.Join(",", pageNodes.Select(static node => node.LogicalId).ToArray());
        var registeredInCatalog = pageCount > 0 || regionCount > 0 || worldNodeCount > 0 || runtimeSceneCount > 0 || topologyCount > 0;
        var planSummary = $"pages={pageCount}, regions={regionCount}, scenes={runtimeSceneCount}, nodes={worldNodeCount}";
        var topologySummary = $"topologies={topologyCount}, nodes={topologyNodeCount}";

        return new LongLiveMapDemoRuntimeSnapshot
        {
            RegistrationEnabled = options.EnableDemoMapRegistration.Value,
            Registered = registeredInCatalog || _runtime.GetInt(LongLiveMapDemoStateKeys.Registered, 0) != 0,
            RegisteredOwnerModInRouting = registration.OwningModIds.Contains(LongLiveMapDemoConstants.OwningModId, StringComparer.Ordinal),
            PlanSummary = ResolveFirstNonEmpty(_runtime.GetString(LongLiveMapDemoStateKeys.PlanSummary, string.Empty), planSummary),
            TopologySummary = ResolveFirstNonEmpty(_runtime.GetString(LongLiveMapDemoStateKeys.TopologySummary, string.Empty), topologySummary),
            RouteKind = _runtime.GetString(LongLiveMapDemoStateKeys.RouteKind, string.Empty),
            NodeSummary = ResolveFirstNonEmpty(_runtime.GetString(LongLiveMapDemoStateKeys.NodeSummary, string.Empty), nodeSummary),
            ResolveStatus = _runtime.GetString(LongLiveMapDemoStateKeys.ResolveStatus, string.Empty),
            PlanningDumpStatus = _runtime.GetString(LongLiveMapDemoStateKeys.PlanningDumpStatus, string.Empty),
            WarpStatus = _runtime.GetString(LongLiveMapDemoStateKeys.WarpStatus, string.Empty),
            OverviewNodeStatus = _runtime.GetString(LongLiveMapDemoStateKeys.OverviewNodeStatus, string.Empty),
            CustomPageStatus = _runtime.GetString(LongLiveMapDemoStateKeys.CustomPageStatus, string.Empty),
            CustomPageRuntimeSummary = BuildCustomPageRuntimeSummary(customPageRuntime),
            RouteCount = registration.RouteCount,
            ProjectionCount = registration.RouteProjectionCount,
            RuntimeSceneCount = registration.CustomRuntimeSceneCount,
            RuntimeBootstrapCount = registration.CustomRuntimeBootstrapCount,
            RegisteredModCount = registration.OwningModIds.Count,
            HasActiveProjection = mapOverview.HasActiveProjection,
            HasRuntimeBootstrap = customRuntime.HasRuntimeBootstrap,
            BindableTargetCount = allocation.BindableTargetCount,
            DedicatedShellCount = allocation.DedicatedShellCount,
            ReservedShellCount = reservation.CreatedReservationCount,
            HiddenReservationCount = reservation.HiddenReservationCount,
            CustomPageTargetCount = customPageRuntime.CustomPageTargetCount,
            CustomPageTabCount = customPageRuntime.MountedTabButtonCount,
            CustomPageRootCount = customPageRuntime.MountedPageRootCount,
            CustomPageRegionOverlayCount = customPageRuntime.ActivePageRegionOverlayCount,
            CustomPageRenderedNodeCount = customPageRuntime.ActivePageRenderedNodeCount,
            TopologyCount = topology.TotalTopologyCount,
            TopologyNodeCount = topology.TotalNodeCount,
        };
    }

    public string ResolveDemoRoute()
    {
        var resolution = LongLivePluginContext.SceneRouting.Resolve(CreateDemoAddress());
        var routeKind = resolution.RouteKind.ToString();
        var summary = BuildResolveSummary(resolution);

        _runtime.SetString(LongLiveMapDemoStateKeys.RouteKind, routeKind);
        _runtime.SetString(LongLiveMapDemoStateKeys.ResolveStatus, summary);

        _logger.LogInfo($"[LongLiveDemoMap] resolve: {summary}");
        return summary;
    }

    public string DumpAllocationState()
    {
        var allocation = LongLivePluginContext.GetMapOverviewShellAllocationRuntimeSnapshot(int.MaxValue);
        var reservation = LongLivePluginContext.GetMapOverviewShellReservationRuntimeSnapshot(int.MaxValue);
        var summary = $"allocationTargets={allocation.TargetCount}, dedicated={allocation.DedicatedShellCount}, bindable={allocation.BindableTargetCount}, reservations={reservation.CreatedReservationCount}, hidden={reservation.HiddenReservationCount}";

        _runtime.SetString(LongLiveMapDemoStateKeys.ResolveStatus, summary);
        _logger.LogInfo($"[LongLiveDemoMap] shell state: {summary}");
        return summary;
    }

    public LongLiveSceneRoutingPlanningDumpResult ExportPlanningDump()
    {
        var service = new LongLiveSceneRoutingPlanningDumpService();
        var result = service.ExportCurrentBundle();
        var status = result.Success
            ? $"success: {result.Path}"
            : $"error: {result.Summary}";

        _runtime.SetString(LongLiveMapDemoStateKeys.PlanningDumpStatus, status);

        if (result.Success)
        {
            _logger.LogInfo($"[LongLiveDemoMap] planning dump exported: {result.Path}");
        }
        else
        {
            _logger.LogWarning($"[LongLiveDemoMap] planning dump failed: {result.Summary}");
        }

        return result;
    }

    public string EnterDemoRuntime()
    {
        var address = CreateDemoAddress();
        var result = LongLivePluginContext.SceneRouting.WarpPlayer(address);
        var status = BuildWarpSummary("enter", result, address);
        _runtime.SetString(LongLiveMapDemoStateKeys.WarpStatus, status);

        if (result.Succeeded)
        {
            _logger.LogInfo($"[LongLiveDemoMap] enter runtime: {status}");
        }
        else
        {
            _logger.LogWarning($"[LongLiveDemoMap] enter runtime failed: {status}");
        }

        return status;
    }

    public string ReturnToAnchorScene()
    {
        var address = new LongLiveSceneAddress
        {
            LogicalSceneId = LongLiveMapDemoConstants.OuterSceneId,
            SceneName = LongLiveMapDemoConstants.OuterSceneName,
            AutoResolveEntryIndex = true,
            PreserveLastScene = true,
        };

        var result = LongLivePluginContext.SceneRouting.WarpPlayer(address);
        var status = BuildWarpSummary("return", result, address);
        _runtime.SetString(LongLiveMapDemoStateKeys.WarpStatus, status);

        if (result.Succeeded)
        {
            _logger.LogInfo($"[LongLiveDemoMap] return to anchor: {status}");
        }
        else
        {
            _logger.LogWarning($"[LongLiveDemoMap] return to anchor failed: {status}");
        }

        return status;
    }

    private static LongLiveSceneAddress CreateDemoAddress()
    {
        return new LongLiveSceneAddress
        {
            LogicalSceneId = LongLiveMapDemoConstants.CustomSceneId,
            SceneName = LongLiveMapDemoConstants.CustomSceneName,
            AutoResolveEntryIndex = true,
        };
    }

    private static string BuildResolveSummary(LongLiveSceneRouteResolution resolution)
    {
        return $"kind={resolution.RouteKind}, logicalId={resolution.Descriptor?.LogicalId ?? "n/a"}, page={resolution.Descriptor?.OverviewPageId ?? "n/a"}";
    }

    private static string BuildWarpSummary(string action, LongLiveSceneRouteResult result, LongLiveSceneAddress address)
    {
        var requestedScene = string.IsNullOrWhiteSpace(address.SceneName) ? "n/a" : address.SceneName;
        var logicalId = string.IsNullOrWhiteSpace(address.LogicalSceneId) ? "n/a" : address.LogicalSceneId;
        var appliedEntry = result.AppliedEntryIndex.HasValue ? result.AppliedEntryIndex.Value.ToString() : "n/a";
        var requestedEntry = address.EntryIndex.HasValue ? address.EntryIndex.Value.ToString() : "auto";

        return result.Succeeded
            ? $"{action}: success, scene={requestedScene}, logicalId={logicalId}, kind={result.RequestedSceneKind}, requestedEntry={requestedEntry}, appliedEntry={appliedEntry}, detail={result.Detail}"
            : $"{action}: failed, scene={requestedScene}, logicalId={logicalId}, kind={result.RequestedSceneKind}, requestedEntry={requestedEntry}, code={result.FailureCode}, detail={result.Detail}";
    }

    private static string ResolveFirstNonEmpty(string primary, string fallback)
    {
        return string.IsNullOrWhiteSpace(primary) ? fallback : primary;
    }

    private static string BuildCustomPageRuntimeSummary(LongLiveMapOverviewCustomPageRuntimeSnapshot snapshot)
    {
        var mountedPages = snapshot.MountedPageIds.Count == 0
            ? "n/a"
            : string.Join(",", snapshot.MountedPageIds.ToArray());
        var activePage = string.IsNullOrWhiteSpace(snapshot.ActivePageId) ? "n/a" : snapshot.ActivePageId;
        return $"panel={snapshot.HasPanelInstance}, active={snapshot.IsCustomPageActive}, page={activePage}, targets={snapshot.CustomPageTargetCount}, tabs={snapshot.MountedTabButtonCount}, roots={snapshot.MountedPageRootCount}, regions={snapshot.ActivePageRegionOverlayCount}, nodes={snapshot.ActivePageRenderedNodeCount}, mounted={mountedPages}";
    }
}
