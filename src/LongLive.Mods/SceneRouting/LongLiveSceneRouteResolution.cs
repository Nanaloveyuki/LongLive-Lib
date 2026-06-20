namespace LongLive.Mods.SceneRouting;

public sealed class LongLiveSceneRouteResolution
{
    public string RequestedSceneName { get; set; } = string.Empty;

    public string RequestedLogicalId { get; set; } = string.Empty;

    public LongLiveSceneRouteDescriptor? Descriptor { get; set; }

    public LongLiveSceneRouteKind RouteKind { get; set; } = LongLiveSceneRouteKind.Unknown;
}
