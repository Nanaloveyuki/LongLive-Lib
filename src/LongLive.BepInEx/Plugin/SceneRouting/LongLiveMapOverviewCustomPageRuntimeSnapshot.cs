using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewCustomPageRuntimeSnapshot
{
    public string ActiveSceneName { get; set; } = string.Empty;

    public bool HasPanelInstance { get; set; }

    public bool HasCachedPanel { get; set; }

    public bool HasTabRoot { get; set; }

    public bool HasPanelRoot { get; set; }

    public bool IsCustomPageActive { get; set; }

    public string ActivePageId { get; set; } = string.Empty;

    public string ActivePageDisplayName { get; set; } = string.Empty;

    public int CustomPageTargetCount { get; set; }

    public int MountedTabButtonCount { get; set; }

    public int MountedTabHighlightCount { get; set; }

    public int MountedPageRootCount { get; set; }

    public int ActivePageRenderedNodeCount { get; set; }

    public int ActivePageRegionOverlayCount { get; set; }

    public List<string> MountedPageIds { get; set; } = new List<string>();
}
