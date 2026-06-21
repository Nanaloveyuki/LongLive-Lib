using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeExecutionReport
{
    public int StepCount { get; set; }

    public int SuccessCount { get; set; }

    public int FailureCount { get; set; }

    public IReadOnlyList<LongLiveCustomMapRuntimeExecutionStepResult> Results { get; set; } = new LongLiveCustomMapRuntimeExecutionStepResult[0];
}
