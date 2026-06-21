using System.Collections.Generic;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveSceneRoutingRegistrationSnapshot
{
    public int RouteCount { get; set; }

    public int WorldMapPageCount { get; set; }

    public int HighlightRegionCount { get; set; }

    public int WorldNodeCount { get; set; }

    public int RouteProjectionCount { get; set; }

    public int CustomRuntimeSceneCount { get; set; }

    public int CustomRuntimeBootstrapCount { get; set; }

    public int SceneLocalTopologyCount { get; set; }

    public int SceneLocalNodeCount { get; set; }

    public IReadOnlyList<string> OwningModIds { get; set; } = new string[0];

    public IReadOnlyDictionary<string, int> RouteCountsByModId { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> PageCountsByModId { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> RegionCountsByModId { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> NodeCountsByModId { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> RouteProjectionCountsByModId { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> RuntimeSceneCountsByModId { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> RuntimeBootstrapCountsByModId { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> TopologyCountsByModId { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> RouteKindCounts { get; set; } = new Dictionary<string, int>();

    public bool HasActiveSceneRegistration { get; set; }

    public string ActiveSceneName { get; set; } = string.Empty;

    public string ActiveSceneLogicalId { get; set; } = string.Empty;

    public string ActiveSceneOwningModId { get; set; } = string.Empty;

    public string ActiveOverviewPageId { get; set; } = string.Empty;

    public string ActiveHighlightRegionId { get; set; } = string.Empty;

    public LongLiveSceneRouteKind ActiveRouteKind { get; set; } = LongLiveSceneRouteKind.Unknown;
}
