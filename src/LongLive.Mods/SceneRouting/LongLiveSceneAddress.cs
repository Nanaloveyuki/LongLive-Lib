namespace LongLive.Mods.SceneRouting;

public sealed class LongLiveSceneAddress
{
    public string LogicalSceneId { get; set; } = string.Empty;

    public string SceneName { get; set; } = string.Empty;

    public LongLiveSceneRouteKind RouteKind { get; set; } = LongLiveSceneRouteKind.Unknown;

    public int? EntryIndex { get; set; }

    public bool AutoResolveEntryIndex { get; set; }

    public bool PreserveLastScene { get; set; } = true;
}
