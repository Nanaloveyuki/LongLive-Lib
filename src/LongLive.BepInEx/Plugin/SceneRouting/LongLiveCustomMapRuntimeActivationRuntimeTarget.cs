namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeActivationRuntimeTarget
{
    public string SceneLogicalId { get; set; } = string.Empty;

    public string SceneName { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool MatchesActiveScene { get; set; }

    public bool RequiresCustomActivation { get; set; }

    public bool IsHostBackedScene { get; set; }

    public bool HasBindableHostProxy { get; set; }

    public bool CanPrepareHostActivationSurface { get; set; }

    public bool CanBindProxyRoute { get; set; }

    public bool CanEnterNow { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string ActivationState { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;
}
