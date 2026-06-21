using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeActivationExecutionPlan
{
    public int StepCount { get; set; }

    public int ExecutableStepCount { get; set; }

    public int PendingStepCount { get; set; }

    public IReadOnlyList<LongLiveCustomMapRuntimeActivationStep> Steps { get; set; } = new LongLiveCustomMapRuntimeActivationStep[0];
}
