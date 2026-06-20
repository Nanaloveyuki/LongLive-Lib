using LongLive.Mods.SceneRouting;

namespace LongLive.Mods.Maps;

public sealed class LongLiveCustomMapRuntimeBootstrapDescriptor
{
    public string SceneLogicalId { get; set; } = string.Empty;

    public string SceneName { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string OverviewPageId { get; set; } = string.Empty;

    public string HighlightRegionId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public LongLiveMapKind MapKind { get; set; } = LongLiveMapKind.Unknown;

    public LongLiveSceneRouteKind RouteKind { get; set; } = LongLiveSceneRouteKind.Unknown;

    public int? HostMapType { get; set; }

    public string EntryNodeLogicalId { get; set; } = string.Empty;

    public int? PreferredEntryIndex { get; set; }

    public string ReturnSceneLogicalId { get; set; } = string.Empty;

    public string ReturnSceneName { get; set; } = string.Empty;

    public int? PreferredReturnEntryIndex { get; set; }

    public string AssetBundleId { get; set; } = string.Empty;

    public bool CanBootstrapRuntime => !string.IsNullOrWhiteSpace(SceneLogicalId) && !string.IsNullOrWhiteSpace(SceneName);

    public LongLiveSceneAddress CreateEntryAddress(bool preserveLastScene = true)
    {
        return new LongLiveSceneAddress
        {
            LogicalSceneId = SceneLogicalId,
            SceneName = SceneName,
            RouteKind = RouteKind,
            EntryIndex = PreferredEntryIndex,
            AutoResolveEntryIndex = !PreferredEntryIndex.HasValue,
            PreserveLastScene = preserveLastScene,
        };
    }

    public LongLiveSceneAddress CreateReturnAddress(bool preserveLastScene = true)
    {
        return new LongLiveSceneAddress
        {
            LogicalSceneId = ReturnSceneLogicalId,
            SceneName = ReturnSceneName,
            EntryIndex = PreferredReturnEntryIndex,
            AutoResolveEntryIndex = !PreferredReturnEntryIndex.HasValue,
            PreserveLastScene = preserveLastScene,
        };
    }
}
