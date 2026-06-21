using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewExecutionPlan
{
    public int StepCount { get; set; }

    public int InjectionStepCount { get; set; }

    public int DryRunStepCount { get; set; }

    public IReadOnlyList<LongLiveMapOverviewExecutionStep> Steps { get; set; } = new LongLiveMapOverviewExecutionStep[0];
}
