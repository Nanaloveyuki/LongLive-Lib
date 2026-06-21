namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewExecutionStepResult
{
    public string StepId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public LongLiveMapOverviewHostBindingSnapshot HostBinding { get; set; } = new LongLiveMapOverviewHostBindingSnapshot();
}
