using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewHostBindingRuntimeSnapshot
{
    public string ActiveSceneName { get; set; } = string.Empty;

    public bool HasUiMapPanel { get; set; }

    public bool HasNingZhouHost { get; set; }

    public bool HasSeaHost { get; set; }

    public bool HasNingZhouAnchor { get; set; }

    public bool HasSeaAnchor { get; set; }

    public int TotalBindingTargetCount { get; set; }

    public int ExternalBindingTargetCount { get; set; }

    public int BindableTargetCount { get; set; }

    public int MissingAnchorTargetCount { get; set; }

    public string PanelObjectName { get; set; } = string.Empty;

    public string ProbeError { get; set; } = string.Empty;

    public List<LongLiveMapOverviewHostBindingTarget> Targets { get; set; } = new List<LongLiveMapOverviewHostBindingTarget>();
}
