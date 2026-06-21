using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeActivationTarget
{
    public string SceneLogicalId { get; set; } = string.Empty;

    public string SceneName { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string OverviewPageId { get; set; } = string.Empty;

    public string HighlightRegionId { get; set; } = string.Empty;

    public string EntryNodeLogicalId { get; set; } = string.Empty;

    public string ReturnSceneLogicalId { get; set; } = string.Empty;

    public string ReturnSceneName { get; set; } = string.Empty;

    public int? PreferredEntryIndex { get; set; }

    public int? PreferredReturnEntryIndex { get; set; }

    public string AssetBundleId { get; set; } = string.Empty;

    public LongLiveSceneRouteKind RouteKind { get; set; } = LongLiveSceneRouteKind.Unknown;

    public bool MatchesActiveScene { get; set; }

    public bool RequiresCustomActivation { get; set; }

    public int SceneLocalTopologyNodeCount { get; set; }

    public LongLiveSceneAddress EntryAddress { get; set; } = new LongLiveSceneAddress();

    public LongLiveSceneAddress ReturnAddress { get; set; } = new LongLiveSceneAddress();
}
