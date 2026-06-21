namespace LongLive.BepInEx.Plugin;

internal sealed class LongLiveMapDemoRuntimeSnapshot
{
    public bool RegistrationEnabled { get; set; }

    public bool Registered { get; set; }

    public bool RegisteredOwnerModInRouting { get; set; }

    public string PlanSummary { get; set; } = string.Empty;

    public string TopologySummary { get; set; } = string.Empty;

    public string RouteKind { get; set; } = string.Empty;

    public string NodeSummary { get; set; } = string.Empty;

    public string ResolveStatus { get; set; } = string.Empty;

    public string PlanningDumpStatus { get; set; } = string.Empty;

    public string WarpStatus { get; set; } = string.Empty;

    public string OverviewNodeStatus { get; set; } = string.Empty;

    public string CustomPageStatus { get; set; } = string.Empty;

    public string CustomPageRuntimeSummary { get; set; } = string.Empty;

    public int RouteCount { get; set; }

    public int ProjectionCount { get; set; }

    public int RuntimeSceneCount { get; set; }

    public int RuntimeBootstrapCount { get; set; }

    public int RegisteredModCount { get; set; }

    public bool HasActiveProjection { get; set; }

    public bool HasRuntimeBootstrap { get; set; }

    public int BindableTargetCount { get; set; }

    public int DedicatedShellCount { get; set; }

    public int ReservedShellCount { get; set; }

    public int HiddenReservationCount { get; set; }

    public int CustomPageTargetCount { get; set; }

    public int CustomPageTabCount { get; set; }

    public int CustomPageRootCount { get; set; }

    public int CustomPageRegionOverlayCount { get; set; }

    public int CustomPageRenderedNodeCount { get; set; }

    public int TopologyCount { get; set; }

    public int TopologyNodeCount { get; set; }
}
