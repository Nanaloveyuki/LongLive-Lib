using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public static class LongLiveMapRegistrySamples
{
    public static LongLiveMapRegistryDraft CreateSeaIslandSample(string modId = "longlive.sample")
    {
        var pageId = modId + ".page.pengsha-sea";
        var regionId = modId + ".region.pengsha";
        var islandSceneId = modId + ".scene.pengsha-island";
        var seaSceneId = modId + ".scene.pengsha-sea";
        var dockNodeId = modId + ".node.pengsha-dock";
        var innerNodeId = modId + ".node.pengsha-inner";

        return new LongLiveMapRegistryDraft
        {
            Pages = new List<LongLiveWorldMapPageDescriptor>
            {
                new LongLiveWorldMapPageDescriptor
                {
                    LogicalId = pageId,
                    OwningModId = modId,
                    DisplayName = "Pengsha Sea",
                    BackgroundAssetId = modId + ".asset.page.pengsha-sea-bg",
                    TabIconAssetId = modId + ".asset.page.pengsha-sea-tab",
                    OrderHint = 200,
                    HighlightRegionIds = new List<string> { regionId },
                    NodeIds = new List<string> { dockNodeId, innerNodeId },
                },
            },
            HighlightRegions = new List<LongLiveHighlightRegionDescriptor>
            {
                new LongLiveHighlightRegionDescriptor
                {
                    LogicalId = regionId,
                    OwningModId = modId,
                    PageId = pageId,
                    DisplayName = "Pengsha Region",
                },
            },
            Scenes = new List<LongLiveSceneDescriptor>
            {
                new LongLiveSceneDescriptor
                {
                    LogicalId = seaSceneId,
                    OwningModId = modId,
                    SceneName = "SeaPengsha01",
                    MapKind = LongLiveMapKind.SeaRegion,
                    DisplayName = "Pengsha Sea",
                    EventName = "Pengsha Sea",
                    OverviewPageId = pageId,
                    HighlightRegionId = regionId,
                },
                new LongLiveSceneDescriptor
                {
                    LogicalId = islandSceneId,
                    OwningModId = modId,
                    SceneName = "SPengsha01",
                    MapKind = LongLiveMapKind.SeaIsland,
                    DisplayName = "Pengsha Island",
                    EventName = "Pengsha Island",
                    OverviewPageId = pageId,
                    HighlightRegionId = regionId,
                    OutsideSceneLogicalId = seaSceneId,
                },
            },
            Nodes = new List<LongLiveWorldNodeDescriptor>
            {
                new LongLiveWorldNodeDescriptor
                {
                    LogicalId = dockNodeId,
                    OwningModId = modId,
                    PageId = pageId,
                    DisplayName = "Pengsha Dock",
                    Position = new LongLiveMapPoint(320f, 240f),
                    MapKind = LongLiveMapKind.SeaIsland,
                    TargetSceneLogicalId = islandSceneId,
                    ConnectedNodeIds = new List<string> { innerNodeId },
                },
                new LongLiveWorldNodeDescriptor
                {
                    LogicalId = innerNodeId,
                    OwningModId = modId,
                    PageId = pageId,
                    DisplayName = "Outer Reef",
                    Position = new LongLiveMapPoint(440f, 210f),
                    MapKind = LongLiveMapKind.SeaRegion,
                    TargetSceneLogicalId = seaSceneId,
                    ConnectedNodeIds = new List<string> { dockNodeId },
                },
            },
        };
    }
}
