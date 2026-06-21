using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewShellAllocationPlan
{
    public int TargetCount { get; set; }

    public int ReuseExistingShellCount { get; set; }

    public int DedicatedShellCount { get; set; }

    public int BindableTargetCount { get; set; }

    public IReadOnlyList<LongLiveMapOverviewShellAllocationTarget> Targets { get; set; } = new LongLiveMapOverviewShellAllocationTarget[0];
}
