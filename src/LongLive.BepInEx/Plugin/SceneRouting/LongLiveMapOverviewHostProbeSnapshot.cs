namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewHostProbeSnapshot
{
    public bool HasUiMapPanel { get; set; }

    public bool HasNingZhou { get; set; }

    public bool HasSea { get; set; }

    public string PanelObjectName { get; set; } = string.Empty;

    public string TabRootObjectName { get; set; } = string.Empty;

    public string MapBackgroundObjectName { get; set; } = string.Empty;

    public string NingZhouNodeRootName { get; set; } = string.Empty;

    public string NingZhouHighlightRootName { get; set; } = string.Empty;

    public string SeaNodeRootName { get; set; } = string.Empty;

    public string SeaHighlightRootName { get; set; } = string.Empty;

    public string SeaNamesRootName { get; set; } = string.Empty;

    public bool HasNingZhouInjectionAnchor { get; set; }

    public bool HasSeaInjectionAnchor { get; set; }

    public string NingZhouInjectionAnchorName { get; set; } = string.Empty;

    public string SeaInjectionAnchorName { get; set; } = string.Empty;

    public int NingZhouNodeChildCount { get; set; }

    public int SeaNodeChildCount { get; set; }

    public int NingZhouHighlightChildCount { get; set; }

    public int SeaHighlightChildCount { get; set; }

    public int SeaNameChildCount { get; set; }

    public string NingZhouHierarchySample { get; set; } = string.Empty;

    public string SeaHierarchySample { get; set; } = string.Empty;

    public string ProbeError { get; set; } = string.Empty;
}
