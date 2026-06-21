namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeReadinessTarget
{
    public string SceneLogicalId { get; set; } = string.Empty;

    public string SceneName { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string EntryNodeLogicalId { get; set; } = string.Empty;

    public string EntrySceneName { get; set; } = string.Empty;

    public string ReturnSceneName { get; set; } = string.Empty;

    public string AssetBundleId { get; set; } = string.Empty;

    public bool MatchesActiveScene { get; set; }

    public bool RequiresCustomActivation { get; set; }

    public bool NeedsCustomActivationImplementation { get; set; }

    public bool IsHostBackedScene { get; set; }

    public bool EntryRouteResolvable { get; set; }

    public bool ReturnRouteResolvable { get; set; }

    public bool ReturnRouteRequired { get; set; }

    public bool PreferredEntryIndexSane { get; set; }

    public bool PreferredReturnEntryIndexSane { get; set; }

    public string EntryRouteKind { get; set; } = string.Empty;

    public string ReturnRouteKind { get; set; } = string.Empty;

    public int TopologyNodeCount { get; set; }

    public bool CanEnterNow { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;
}
