namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeExecutionStepResult
{
    public string StepId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public LongLiveCustomMapRuntimeHostBindingSnapshot HostBinding { get; set; } = new LongLiveCustomMapRuntimeHostBindingSnapshot();
}
