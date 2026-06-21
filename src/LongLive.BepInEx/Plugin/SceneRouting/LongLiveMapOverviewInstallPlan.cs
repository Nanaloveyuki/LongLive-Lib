using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewInstallPlan
{
    public string ActiveSceneName { get; set; } = string.Empty;

    public string ActiveSceneLogicalId { get; set; } = string.Empty;

    public string ActiveOwningModId { get; set; } = string.Empty;

    public bool HasActivePageTarget { get; set; }

    public string ActivePageId { get; set; } = string.Empty;

    public string ActiveRegionId { get; set; } = string.Empty;

    public int PageTargetCount { get; set; }

    public int RegionTargetCount { get; set; }

    public int NodeTargetCount { get; set; }

    public int ProjectionCount { get; set; }

    public int ExternalPageTargetCount { get; set; }

    public int ExternalProjectionCount { get; set; }

    public IReadOnlyList<LongLiveMapOverviewPageInstallTarget> PageTargets { get; set; } = new LongLiveMapOverviewPageInstallTarget[0];
}
