namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewHostBindingSnapshot
{
    public string PageId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public bool ExpectsSeaHost { get; set; }

    public bool HasExpectedHostRoot { get; set; }

    public bool HasInjectionAnchor { get; set; }

    public string InjectionAnchorName { get; set; } = string.Empty;

    public string HostRootName { get; set; } = string.Empty;

    public string HighlightRootName { get; set; } = string.Empty;

    public int NodeChildCount { get; set; }

    public int HighlightChildCount { get; set; }

    public string HierarchySample { get; set; } = string.Empty;
}
