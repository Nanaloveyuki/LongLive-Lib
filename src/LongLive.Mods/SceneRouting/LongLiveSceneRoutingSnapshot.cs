namespace LongLive.Mods.SceneRouting;

public sealed class LongLiveSceneRoutingSnapshot
{
    public string ActiveSceneName { get; set; } = string.Empty;

    public LongLiveSceneRouteKind ActiveSceneKind { get; set; } = LongLiveSceneRouteKind.Unknown;

    public string PlaceName { get; set; } = string.Empty;

    public int? PlayerNowMapIndex { get; set; }

    public string PlayerLastScene { get; set; } = string.Empty;

    public string PlayerLastFuBenScene { get; set; } = string.Empty;

    public string PlayerNowFuBen { get; set; } = string.Empty;

    public int? CurrentFuBenIndex { get; set; }
}
