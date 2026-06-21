namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeActivationArtifact
{
    public string SceneLogicalId { get; set; } = string.Empty;

    public string SceneName { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string ActivationState { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;

    public bool HasPreparedHostSurface { get; set; }

    public bool HasProxyRouteBinding { get; set; }

    public string BoundEntrySceneName { get; set; } = string.Empty;

    public string BoundReturnSceneName { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;
}
