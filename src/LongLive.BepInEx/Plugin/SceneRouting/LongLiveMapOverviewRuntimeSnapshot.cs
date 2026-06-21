using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewRuntimeSnapshot
{
    public string ActiveSceneName { get; set; } = string.Empty;

    public bool HasActiveProjection { get; set; }

    public string ActiveSceneLogicalId { get; set; } = string.Empty;

    public string ActiveOwningModId { get; set; } = string.Empty;

    public string ActivePageId { get; set; } = string.Empty;

    public string ActiveRegionId { get; set; } = string.Empty;

    public int TotalPageCount { get; set; }

    public int TotalRegionCount { get; set; }

    public int TotalNodeCount { get; set; }

    public int TotalProjectionCount { get; set; }

    public int ExternalPageTargetCount { get; set; }

    public int ExternalProjectionCount { get; set; }

    public List<string> RegisteredModIds { get; set; } = new List<string>();

    public List<string> ActivePageNodeNames { get; set; } = new List<string>();

    public List<string> ActiveRegionNodeNames { get; set; } = new List<string>();

    public LongLiveMapOverviewHostBindingRuntimeSnapshot HostBindingRuntime { get; set; } = new LongLiveMapOverviewHostBindingRuntimeSnapshot();

    public LongLiveMapOverviewCustomPageRuntimeSnapshot CustomPageRuntime { get; set; } = new LongLiveMapOverviewCustomPageRuntimeSnapshot();

    public LongLiveMapOverviewInstallPlan InstallPlan { get; set; } = new LongLiveMapOverviewInstallPlan();
}
