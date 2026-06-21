using LongLive.Mods.Maps;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapDemoTopologyFactory
{
    public static LongLiveSceneLocalTopologyBatch CreateBatch()
    {
        return new LongLiveSceneLocalTopologyBatch
        {
            Topologies =
            {
                new LongLiveSceneLocalTopologyDescriptor
                {
                    LogicalId = LongLiveMapDemoConstants.TopologyId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    SceneLogicalId = LongLiveMapDemoConstants.CustomSceneId,
                    SceneName = LongLiveMapDemoConstants.CustomSceneName,
                    DisplayName = "Sky Isle Runtime Topology",
                },
                new LongLiveSceneLocalTopologyDescriptor
                {
                    LogicalId = LongLiveMapDemoConstants.SecondTopologyId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    SceneLogicalId = LongLiveMapDemoConstants.SecondCustomSceneId,
                    SceneName = LongLiveMapDemoConstants.SecondCustomSceneName,
                    DisplayName = "Crimson Cove Runtime Topology",
                },
            },
            Nodes =
            {
                new LongLiveSceneLocalNodeDescriptor
                {
                    LogicalId = "longlive.demo.topology.node.arrival",
                    TopologyLogicalId = LongLiveMapDemoConstants.TopologyId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    DisplayName = "Arrival Platform",
                    Position = new LongLiveMapPoint(0f, 0f),
                    IsCity = false,
                },
                new LongLiveSceneLocalNodeDescriptor
                {
                    LogicalId = "longlive.demo.topology.node.market",
                    TopologyLogicalId = LongLiveMapDemoConstants.TopologyId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    DisplayName = "Cloud Market",
                    Position = new LongLiveMapPoint(128f, 64f),
                    IsCity = true,
                    ConnectedNodeIds = { "longlive.demo.topology.node.arrival" },
                },
                new LongLiveSceneLocalNodeDescriptor
                {
                    LogicalId = "longlive.demo.topology.node.lookout",
                    TopologyLogicalId = LongLiveMapDemoConstants.TopologyId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    DisplayName = "Star Lookout",
                    Position = new LongLiveMapPoint(220f, 148f),
                    IsHidden = true,
                    ConnectedNodeIds = { "longlive.demo.topology.node.market" },
                },
                new LongLiveSceneLocalNodeDescriptor
                {
                    LogicalId = "longlive.demo.topology.node.crimson.arrival",
                    TopologyLogicalId = LongLiveMapDemoConstants.SecondTopologyId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    DisplayName = "Crimson Dock",
                    Position = new LongLiveMapPoint(0f, 0f),
                    IsCity = false,
                },
                new LongLiveSceneLocalNodeDescriptor
                {
                    LogicalId = "longlive.demo.topology.node.crimson.shrine",
                    TopologyLogicalId = LongLiveMapDemoConstants.SecondTopologyId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    DisplayName = "Ash Shrine",
                    Position = new LongLiveMapPoint(96f, 84f),
                    IsCity = true,
                    ConnectedNodeIds = { "longlive.demo.topology.node.crimson.arrival" },
                },
                new LongLiveSceneLocalNodeDescriptor
                {
                    LogicalId = "longlive.demo.topology.node.crimson.depths",
                    TopologyLogicalId = LongLiveMapDemoConstants.SecondTopologyId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    DisplayName = "Red Depths",
                    Position = new LongLiveMapPoint(188f, 126f),
                    IsHidden = false,
                    ConnectedNodeIds = { "longlive.demo.topology.node.crimson.shrine" },
                },
            },
        };
    }
}
