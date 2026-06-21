using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewPageInstallTarget
{
    public string PageId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string BackgroundAssetId { get; set; } = string.Empty;

    public string TabIconAssetId { get; set; } = string.Empty;

    public int? OrderHint { get; set; }

    public int RegionCount { get; set; }

    public int NodeCount { get; set; }

    public int ProjectionCount { get; set; }

    public bool ContainsActiveProjection { get; set; }

    public bool RequiresHostInjection { get; set; }

    public IReadOnlyList<string> RegionIds { get; set; } = new string[0];

    public IReadOnlyList<string> NodeIds { get; set; } = new string[0];

    public IReadOnlyList<string> ProjectionSceneLogicalIds { get; set; } = new string[0];
}
