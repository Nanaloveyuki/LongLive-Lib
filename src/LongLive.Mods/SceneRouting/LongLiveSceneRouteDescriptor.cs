namespace LongLive.Mods.SceneRouting;

public sealed class LongLiveSceneRouteDescriptor
{
    public string LogicalId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string SceneName { get; set; } = string.Empty;

    public LongLiveSceneRouteKind RouteKind { get; set; } = LongLiveSceneRouteKind.Unknown;

    public string DisplayName { get; set; } = string.Empty;

    public string OverviewPageId { get; set; } = string.Empty;

    public string HighlightRegionId { get; set; } = string.Empty;

    public string AssetBundleId { get; set; } = string.Empty;

    public int? HostMapType { get; set; }

    public int? HostOutsideScenePos { get; set; }

    public string OutsideSceneName { get; set; } = string.Empty;

    public string OutsideSceneLogicalId { get; set; } = string.Empty;
}
