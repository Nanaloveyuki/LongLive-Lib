namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewExecutionStep
{
    public string StepId { get; set; } = string.Empty;

    public string PageId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public bool IsDryRunOnly { get; set; }
}
