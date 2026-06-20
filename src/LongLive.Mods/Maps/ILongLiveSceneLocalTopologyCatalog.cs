using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public interface ILongLiveSceneLocalTopologyCatalog
{
    IReadOnlyCollection<LongLiveSceneLocalTopologyDescriptor> Topologies { get; }

    IReadOnlyCollection<LongLiveSceneLocalNodeDescriptor> Nodes { get; }

    int TopologyCount { get; }

    int NodeCount { get; }

    bool TryGetTopology(string logicalId, out LongLiveSceneLocalTopologyDescriptor? descriptor);

    bool TryGetTopologyBySceneLogicalId(string sceneLogicalId, out LongLiveSceneLocalTopologyDescriptor? descriptor);

    bool TryGetTopologyBySceneName(string sceneName, out LongLiveSceneLocalTopologyDescriptor? descriptor);

    IReadOnlyList<LongLiveSceneLocalTopologyDescriptor> GetTopologiesForMod(string owningModId);

    IReadOnlyList<LongLiveSceneLocalNodeDescriptor> GetNodesForTopology(string topologyLogicalId);
}
