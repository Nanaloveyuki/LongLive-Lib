using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeActivationExecutionReport
{
    public int StepCount { get; set; }

    public int SuccessCount { get; set; }

    public int FailureCount { get; set; }

    public IReadOnlyList<LongLiveCustomMapRuntimeActivationStepResult> Results { get; set; } = new LongLiveCustomMapRuntimeActivationStepResult[0];
}
