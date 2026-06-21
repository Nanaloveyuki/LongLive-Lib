namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeActivationStepResult
{
    public string StepId { get; set; } = string.Empty;

    public string SceneLogicalId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string ActivationState { get; set; } = string.Empty;

    public bool ProducedHostPreparation { get; set; }

    public bool ProducedProxyBinding { get; set; }
}
