using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewExecutionReport
{
    public int StepCount { get; set; }

    public int SuccessCount { get; set; }

    public int FailureCount { get; set; }

    public LongLiveMapOverviewHostProbeSnapshot HostProbe { get; set; } = new LongLiveMapOverviewHostProbeSnapshot();

    public IReadOnlyList<LongLiveMapOverviewExecutionStepResult> Results { get; set; } = new LongLiveMapOverviewExecutionStepResult[0];
}
