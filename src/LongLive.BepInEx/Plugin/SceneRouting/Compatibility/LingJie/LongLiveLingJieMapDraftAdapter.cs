using System;
using System.Collections.Generic;
using LongLive.Mods.Maps;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveLingJieMapDraftAdapter : ILongLiveThirdPartyMapDraftAdapter
{
    private const string ModId = "maijiu.lingjie";
    private const string NingZhouPageId = ModId + ".page.ningzhou-extension";
    private const string SeaPageId = ModId + ".page.sea-extension";
    private const string NingZhouRegionId = ModId + ".region.ningzhou-extension";
    private const string SeaRegionId = ModId + ".region.sea-extension";
    private const string TianYangAccessRuleSummary = "Unlocked when one of LingJie's Tianyang-city route conditions is satisfied: Xiaoshan tutorial cleared, Xiuluo Valley cleared, Moon Pool encounter started, GZDF >= 2, or sect is 380/390/451/836.";

    public string AdapterId => "lingjie.map-draft";

    public string SourceModId => ModId;

    public bool CanBuildDraft()
    {
        return Type.GetType("MaiJiu.MCS.LingJie.Patch.UIMapNingZhouPatch, LingJie") is not null
            || Type.GetType("MaiJiu.MCS.LingJie.Patch.UIMapSeaPatch, LingJie") is not null;
    }

    public LongLiveMapRegistryDraft BuildDraft()
    {
        var draft = new LongLiveMapRegistryDraft();
        AddNingZhouExtension(draft);
        AddSeaExtension(draft);
        return draft;
    }

    private static void AddNingZhouExtension(LongLiveMapRegistryDraft draft)
    {
        const string sceneLogicalId = ModId + ".scene.tianyang-city";
        const string nodeLogicalId = ModId + ".node.tianyang-city";

        draft.Pages.Add(new LongLiveWorldMapPageDescriptor
        {
            LogicalId = NingZhouPageId,
            OwningModId = ModId,
            DisplayName = "LingJie Ningzhou Extension",
            OrderHint = 6600,
            HighlightRegionIds = new List<string> { NingZhouRegionId },
            NodeIds = new List<string> { nodeLogicalId },
        });

        draft.HighlightRegions.Add(new LongLiveHighlightRegionDescriptor
        {
            LogicalId = NingZhouRegionId,
            OwningModId = ModId,
            PageId = NingZhouPageId,
            DisplayName = "LingJie Ningzhou Region",
        });

        draft.Scenes.Add(new LongLiveSceneDescriptor
        {
            LogicalId = sceneLogicalId,
            OwningModId = ModId,
            SceneName = "S6603",
            MapKind = LongLiveMapKind.Town,
            DisplayName = "天阳城",
            EventName = "天阳城",
            OverviewPageId = NingZhouPageId,
            HighlightRegionId = NingZhouRegionId,
        });

        draft.Nodes.Add(new LongLiveWorldNodeDescriptor
        {
            LogicalId = nodeLogicalId,
            OwningModId = ModId,
            PageId = NingZhouPageId,
            DisplayName = "天阳城",
            Position = new LongLiveMapPoint(1500f, 300f),
            MapKind = LongLiveMapKind.Town,
            TargetSceneLogicalId = sceneLogicalId,
            TargetSceneName = "S6603",
            AccessRuleSummary = TianYangAccessRuleSummary,
        });
    }

    private static void AddSeaExtension(LongLiveMapRegistryDraft draft)
    {
        const string moonPoolSceneId = ModId + ".scene.yuechi-kingdom";
        const string fengDuSceneId = ModId + ".scene.fengdu";
        const string tianYangSceneId = ModId + ".scene.tianyang-city.sea-route";
        const string moonPoolNodeId = ModId + ".node.yuechi-kingdom";
        const string fengDuNodeId = ModId + ".node.fengdu";
        const string tianYangNodeId = ModId + ".node.tianyang-city.sea-route";

        draft.Pages.Add(new LongLiveWorldMapPageDescriptor
        {
            LogicalId = SeaPageId,
            OwningModId = ModId,
            DisplayName = "LingJie Sea Extension",
            OrderHint = 6700,
            HighlightRegionIds = new List<string> { SeaRegionId },
            NodeIds = new List<string> { moonPoolNodeId, fengDuNodeId, tianYangNodeId },
        });

        draft.HighlightRegions.Add(new LongLiveHighlightRegionDescriptor
        {
            LogicalId = SeaRegionId,
            OwningModId = ModId,
            PageId = SeaPageId,
            DisplayName = "LingJie Sea Region",
        });

        draft.Scenes.Add(new LongLiveSceneDescriptor
        {
            LogicalId = moonPoolSceneId,
            OwningModId = ModId,
            SceneName = "S1877",
            MapKind = LongLiveMapKind.SeaIsland,
            DisplayName = "月池国",
            EventName = "月池国",
            OverviewPageId = SeaPageId,
            HighlightRegionId = SeaRegionId,
        });

        draft.Scenes.Add(new LongLiveSceneDescriptor
        {
            LogicalId = fengDuSceneId,
            OwningModId = ModId,
            SceneName = "S1880",
            MapKind = LongLiveMapKind.SeaIsland,
            DisplayName = "酆都",
            EventName = "酆都",
            OverviewPageId = SeaPageId,
            HighlightRegionId = SeaRegionId,
        });

        draft.Scenes.Add(new LongLiveSceneDescriptor
        {
            LogicalId = tianYangSceneId,
            OwningModId = ModId,
            SceneName = "S6603",
            MapKind = LongLiveMapKind.SeaIsland,
            DisplayName = "天阳城",
            EventName = "天阳城",
            OverviewPageId = SeaPageId,
            HighlightRegionId = SeaRegionId,
        });

        draft.Nodes.Add(new LongLiveWorldNodeDescriptor
        {
            LogicalId = moonPoolNodeId,
            OwningModId = ModId,
            PageId = SeaPageId,
            DisplayName = "月池国",
            Position = new LongLiveMapPoint(380f, 50f),
            MapKind = LongLiveMapKind.SeaIsland,
            TargetSceneLogicalId = moonPoolSceneId,
            TargetSceneName = "S1877",
            HostNodeIndex = 10651,
            AccessStaticValueId = 2395,
            HideOnLock = false,
        });

        draft.Nodes.Add(new LongLiveWorldNodeDescriptor
        {
            LogicalId = fengDuNodeId,
            OwningModId = ModId,
            PageId = SeaPageId,
            DisplayName = "酆都",
            Position = new LongLiveMapPoint(1150f, 50f),
            MapKind = LongLiveMapKind.SeaIsland,
            TargetSceneLogicalId = fengDuSceneId,
            TargetSceneName = "S1880",
            HostNodeIndex = 10693,
            AccessStaticValueId = 2396,
            HideOnLock = false,
        });

        draft.Nodes.Add(new LongLiveWorldNodeDescriptor
        {
            LogicalId = tianYangNodeId,
            OwningModId = ModId,
            PageId = SeaPageId,
            DisplayName = "天阳城",
            Position = new LongLiveMapPoint(760f, 30f),
            MapKind = LongLiveMapKind.SeaIsland,
            TargetSceneLogicalId = tianYangSceneId,
            TargetSceneName = "S6603",
            HostNodeIndex = 9321,
            AccessStaticValueId = 2397,
            HideOnLock = false,
        });
    }
}
