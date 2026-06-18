namespace LongLive.Mods.Maps;

public sealed class LongLiveSceneDescriptor
{
    public string LogicalId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string SceneName { get; set; } = string.Empty;

    public LongLiveMapKind MapKind { get; set; } = LongLiveMapKind.Unknown;

    public string DisplayName { get; set; } = string.Empty;

    public string EventName { get; set; } = string.Empty;

    public string OverviewPageId { get; set; } = string.Empty;

    public string HighlightRegionId { get; set; } = string.Empty;

    public string OutsideSceneLogicalId { get; set; } = string.Empty;

    public string OutsideSceneName { get; set; } = string.Empty;

    public string AssetBundleId { get; set; } = string.Empty;

    public int? HostMapType { get; set; }

    public int? HostHighlightId { get; set; }

    public int? HostOutsideScenePos { get; set; }
}
