using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeExecutionPlan
{
    public int StepCount { get; set; }

    public int ActivationStepCount { get; set; }

    public int DryRunStepCount { get; set; }

    public IReadOnlyList<LongLiveCustomMapRuntimeExecutionStep> Steps { get; set; } = new LongLiveCustomMapRuntimeExecutionStep[0];
}
