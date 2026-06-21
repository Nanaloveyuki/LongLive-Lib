namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewHostBindingTarget
{
    public string PageId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool RequiresHostInjection { get; set; }

    public bool ExpectsSeaHost { get; set; }

    public bool HasExpectedHostRoot { get; set; }

    public bool HasInjectionAnchor { get; set; }

    public string InjectionAnchorName { get; set; } = string.Empty;

    public string HostRootName { get; set; } = string.Empty;

    public string HighlightRootName { get; set; } = string.Empty;

    public int NodeChildCount { get; set; }

    public int HighlightChildCount { get; set; }

    public int ProjectionCount { get; set; }

    public int NodeCount { get; set; }

    public int RegionCount { get; set; }

    public bool IsActivePageTarget { get; set; }

    public string HierarchySample { get; set; } = string.Empty;
}
