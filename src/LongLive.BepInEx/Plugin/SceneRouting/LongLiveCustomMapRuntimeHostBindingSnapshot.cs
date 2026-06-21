namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeHostBindingSnapshot
{
    public string SceneLogicalId { get; set; } = string.Empty;

    public string SceneName { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public bool EntryRouteResolvable { get; set; }

    public bool ReturnRouteResolvable { get; set; }

    public bool ReturnRouteRequired { get; set; }

    public bool PreferredEntryIndexSane { get; set; }

    public bool PreferredReturnEntryIndexSane { get; set; }

    public string EntryRouteKind { get; set; } = string.Empty;

    public string ReturnRouteKind { get; set; } = string.Empty;

    public int TopologyNodeCount { get; set; }

    public string EntrySceneName { get; set; } = string.Empty;

    public string ReturnSceneName { get; set; } = string.Empty;
}
