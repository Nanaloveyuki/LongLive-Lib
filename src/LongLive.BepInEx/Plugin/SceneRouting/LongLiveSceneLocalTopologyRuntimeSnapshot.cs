using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveSceneLocalTopologyRuntimeSnapshot
{
    public string ActiveSceneName { get; set; } = string.Empty;

    public bool HasSceneRegistration { get; set; }

    public string RegisteredSceneLogicalId { get; set; } = string.Empty;

    public bool HasActiveTopology { get; set; }

    public string TopologyLogicalId { get; set; } = string.Empty;

    public string TopologyDisplayName { get; set; } = string.Empty;

    public int TotalTopologyCount { get; set; }

    public int TotalNodeCount { get; set; }

    public int ActiveTopologyNodeCount { get; set; }

    public List<string> ActiveNodeNames { get; set; } = new List<string>();
}
