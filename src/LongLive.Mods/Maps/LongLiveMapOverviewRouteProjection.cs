using LongLive.Mods.SceneRouting;

namespace LongLive.Mods.Maps;

public sealed class LongLiveMapOverviewRouteProjection
{
    public string OwningModId { get; set; } = string.Empty;

    public string PageId { get; set; } = string.Empty;

    public string RegionId { get; set; } = string.Empty;

    public string NodeLogicalId { get; set; } = string.Empty;

    public string NodeDisplayName { get; set; } = string.Empty;

    public int? HostNodeIndex { get; set; }

    public string SceneLogicalId { get; set; } = string.Empty;

    public string SceneName { get; set; } = string.Empty;

    public string SceneDisplayName { get; set; } = string.Empty;

    public LongLiveSceneRouteKind RouteKind { get; set; } = LongLiveSceneRouteKind.Unknown;

    public int? PreferredEntryIndex { get; set; }

    public int? AccessStaticValueId { get; set; }

    public bool? HideOnLock { get; set; }

    public string AccessRuleSummary { get; set; } = string.Empty;

    public LongLiveSceneAddress CreateSceneAddress(bool preserveLastScene = true)
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
}
