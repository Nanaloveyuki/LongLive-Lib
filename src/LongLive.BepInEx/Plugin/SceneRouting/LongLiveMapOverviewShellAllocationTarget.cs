namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewShellAllocationTarget
{
    public string PageId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ShellKind { get; set; } = string.Empty;

    public string HostSurface { get; set; } = string.Empty;

    public bool RequiresHostInjection { get; set; }

    public bool CanReuseExistingHostShell { get; set; }

    public bool RequiresDedicatedShell { get; set; }

    public bool CanBindInCurrentSession { get; set; }

    public string AllocationReason { get; set; } = string.Empty;

    public string AnchorName { get; set; } = string.Empty;

    public int ProjectionCount { get; set; }

    public int RegionCount { get; set; }

    public int NodeCount { get; set; }
}
