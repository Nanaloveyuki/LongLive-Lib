using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveSceneLocalTopologyBatch
{
    public List<LongLiveSceneLocalTopologyDescriptor> Topologies { get; set; } = new List<LongLiveSceneLocalTopologyDescriptor>();

    public List<LongLiveSceneLocalNodeDescriptor> Nodes { get; set; } = new List<LongLiveSceneLocalNodeDescriptor>();
}
