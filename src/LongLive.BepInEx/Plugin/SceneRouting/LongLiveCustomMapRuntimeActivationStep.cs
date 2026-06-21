namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeActivationStep
{
    public string StepId { get; set; } = string.Empty;

    public string SceneLogicalId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public bool IsExecutable { get; set; }
}
