using LongLive.Mods.Maps;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapDemoDraftFactory
{
    public static LongLiveMapRegistryDraft CreateDraft()
    {
        return new LongLiveMapRegistryDraft
        {
            Pages =
            {
                new LongLiveWorldMapPageDescriptor
                {
                    LogicalId = LongLiveMapDemoConstants.PageId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    DisplayName = "Sky Isle",
                    BackgroundAssetId = "asset.longlive.demo.sky_isle.background",
                    TabIconAssetId = "asset.longlive.demo.sky_isle.tab",
                },
                new LongLiveWorldMapPageDescriptor
                {
                    LogicalId = LongLiveMapDemoConstants.SecondPageId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    DisplayName = "Crimson Cove",
                    BackgroundAssetId = "asset.longlive.demo.crimson_cove.background",
                    TabIconAssetId = "asset.longlive.demo.crimson_cove.tab",
                    OrderHint = 10,
                },
            },
            HighlightRegions =
            {
                new LongLiveHighlightRegionDescriptor
                {
                    LogicalId = LongLiveMapDemoConstants.RegionId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    PageId = LongLiveMapDemoConstants.PageId,
                    DisplayName = "Sky Isle Region",
                },
                new LongLiveHighlightRegionDescriptor
                {
                    LogicalId = LongLiveMapDemoConstants.SecondRegionId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    PageId = LongLiveMapDemoConstants.SecondPageId,
                    DisplayName = "Crimson Cove Region",
                },
            },
            Scenes =
            {
                new LongLiveSceneDescriptor
                {
                    LogicalId = LongLiveMapDemoConstants.OuterSceneId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    SceneName = LongLiveMapDemoConstants.OuterSceneName,
                    MapKind = LongLiveMapKind.Town,
                    DisplayName = "South Cliff Anchor",
                    EventName = "Demo South Cliff Anchor",
                    OverviewPageId = LongLiveMapDemoConstants.PageId,
                    HighlightRegionId = LongLiveMapDemoConstants.RegionId,
                },
                new LongLiveSceneDescriptor
                {
                    LogicalId = LongLiveMapDemoConstants.CustomSceneId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    SceneName = LongLiveMapDemoConstants.CustomSceneName,
                    MapKind = LongLiveMapKind.Dungeon,
                    DisplayName = "Sky Isle Runtime",
                    EventName = "Demo Sky Isle Runtime",
                    OverviewPageId = LongLiveMapDemoConstants.PageId,
                    HighlightRegionId = LongLiveMapDemoConstants.RegionId,
                    OutsideSceneLogicalId = LongLiveMapDemoConstants.OuterSceneId,
                    OutsideSceneName = LongLiveMapDemoConstants.OuterSceneName,
                    AssetBundleId = "bundle.longlive.demo.sky_isle",
                },
            },
            Nodes =
            {
                new LongLiveWorldNodeDescriptor
                {
                    LogicalId = LongLiveMapDemoConstants.WorldNodeId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    PageId = LongLiveMapDemoConstants.PageId,
                    DisplayName = "Sky Gate",
                    Position = new LongLiveMapPoint(428f, 236f),
                    MapKind = LongLiveMapKind.Dungeon,
                    TargetSceneLogicalId = LongLiveMapDemoConstants.CustomSceneId,
                    TargetSceneName = LongLiveMapDemoConstants.CustomSceneName,
                    ConnectedNodeIds = { LongLiveMapDemoConstants.WorldNodeId },
                    AccessRuleSummary = "Demo node registered by LongLive host map API.",
                },
                new LongLiveWorldNodeDescriptor
                {
                    LogicalId = LongLiveMapDemoConstants.SecondWorldNodeId,
                    OwningModId = LongLiveMapDemoConstants.OwningModId,
                    PageId = LongLiveMapDemoConstants.SecondPageId,
                    DisplayName = "Crimson Portal",
                    Position = new LongLiveMapPoint(196f, 92f),
                    MapKind = LongLiveMapKind.Dungeon,
                    TargetSceneLogicalId = LongLiveMapDemoConstants.CustomSceneId,
                    TargetSceneName = LongLiveMapDemoConstants.CustomSceneName,
                    ConnectedNodeIds = { LongLiveMapDemoConstants.SecondWorldNodeId },
                    AccessRuleSummary = "Second demo page reuses the stable Sky Isle runtime through the same LongLive map overview pipeline.",
                },
            },
        };
    }
}
