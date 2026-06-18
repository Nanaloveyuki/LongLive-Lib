using System;
using System.Collections.Generic;
using JSONClass;
using LongLive.Mods.Maps;
using UnityEngine;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapSnapshotAdapter
{
    public LongLiveMapRegistryDraft CaptureCurrentSnapshot()
    {
        var draft = new LongLiveMapRegistryDraft();

        CaptureSceneMetadata(draft);
        CaptureWorldNodes(draft);
        CaptureOverviewUi(draft);

        return draft;
    }

    private static void CaptureSceneMetadata(LongLiveMapRegistryDraft draft)
    {
        foreach (var metadata in SceneNameJsonData.DataList)
        {
            var sceneLogicalId = "host.scene." + metadata.id;
            var pageId = ResolveOverviewPageId(metadata.HighlightID, metadata.MapType);
            var regionId = metadata.HighlightID > 0 ? "host.region." + metadata.HighlightID : string.Empty;

            draft.Scenes.Add(new LongLiveSceneDescriptor
            {
                LogicalId = sceneLogicalId,
                OwningModId = "host.base-game",
                SceneName = metadata.id,
                MapKind = ResolveMapKind(metadata.MapType),
                DisplayName = metadata.MapName ?? string.Empty,
                EventName = metadata.EventName ?? string.Empty,
                OverviewPageId = pageId,
                HighlightRegionId = regionId,
                OutsideSceneName = metadata.OutsideSceneName ?? string.Empty,
                HostMapType = metadata.MapType,
                HostHighlightId = metadata.HighlightID > 0 ? metadata.HighlightID : null,
                HostOutsideScenePos = metadata.OutsideScenePos > 0 ? metadata.OutsideScenePos : null,
            });

            if (metadata.HighlightID > 0 && !ContainsRegion(draft.HighlightRegions, regionId))
            {
                draft.HighlightRegions.Add(new LongLiveHighlightRegionDescriptor
                {
                    LogicalId = regionId,
                    OwningModId = "host.base-game",
                    PageId = pageId,
                    DisplayName = metadata.MapName ?? metadata.EventName ?? metadata.id,
                    HostHighlightId = metadata.HighlightID,
                });
            }
        }
    }

    private static void CaptureWorldNodes(LongLiveMapRegistryDraft draft)
    {
        var allMapManage = AllMapManage.instance;
        if (allMapManage is null)
        {
            return;
        }

        foreach (var pair in allMapManage.mapIndex)
        {
            var node = pair.Value;
            var pageId = ResolveOverviewPageIdForNode(node);
            var targetSceneName = ResolveTargetSceneName(node.NodeIndex);
            draft.Nodes.Add(new LongLiveWorldNodeDescriptor
            {
                LogicalId = "host.node." + node.NodeIndex,
                OwningModId = "host.base-game",
                PageId = pageId,
                DisplayName = ResolveNodeDisplayName(node.NodeIndex),
                Position = new LongLiveMapPoint(node.transform.position.x, node.transform.position.y),
                MapKind = ResolveNodeMapKind(node.NodeIndex, targetSceneName),
                TargetSceneLogicalId = string.IsNullOrWhiteSpace(targetSceneName) ? string.Empty : "host.scene." + targetSceneName,
                TargetSceneName = targetSceneName,
                NodeGroup = node is MapComponent mapComponent ? mapComponent.NodeGroup : (int?)null,
                ConnectedNodeIds = BuildConnectedNodeIds(node.nextIndex),
                HostNodeIndex = node.NodeIndex,
            });
        }
    }

    private static void CaptureOverviewUi(LongLiveMapRegistryDraft draft)
    {
        var panel = UIMapPanel.Inst;
        if (panel is null)
        {
            return;
        }

        EnsurePage(draft, "host.page.ningzhou", "host.base-game", "Ningzhou", panel.NingZhou?.BGSprite);
        EnsurePage(draft, "host.page.sea", "host.base-game", "Sea", panel.Sea?.BGSprite);
    }

    private static void EnsurePage(LongLiveMapRegistryDraft draft, string logicalId, string owner, string displayName, Sprite? background)
    {
        foreach (var page in draft.Pages)
        {
            if (string.Equals(page.LogicalId, logicalId, StringComparison.Ordinal))
            {
                return;
            }
        }

        draft.Pages.Add(new LongLiveWorldMapPageDescriptor
        {
            LogicalId = logicalId,
            OwningModId = owner,
            DisplayName = displayName,
            BackgroundAssetId = background is null ? string.Empty : "host.asset." + background.name,
        });
    }

    private static bool ContainsRegion(IReadOnlyList<LongLiveHighlightRegionDescriptor> regions, string logicalId)
    {
        foreach (var region in regions)
        {
            if (string.Equals(region.LogicalId, logicalId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolveOverviewPageId(int highlightId, int mapType)
    {
        if (highlightId >= 100 || mapType == 20 || mapType == 7)
        {
            return "host.page.sea";
        }

        return "host.page.ningzhou";
    }

    private static string ResolveOverviewPageIdForNode(BaseMapCompont node)
    {
        if (node is MapSeaCompent)
        {
            return "host.page.sea";
        }

        return "host.page.ningzhou";
    }

    private static List<string> BuildConnectedNodeIds(List<int> nextIndex)
    {
        var result = new List<string>();
        if (nextIndex is null)
        {
            return result;
        }

        foreach (var value in nextIndex)
        {
            result.Add("host.node." + value);
        }

        return result;
    }

    private static string ResolveNodeDisplayName(int nodeIndex)
    {
        if (AllMapLuDainType.DataDict.TryGetValue(nodeIndex, out var metadata) && metadata is not null)
        {
            return metadata.LuDianName ?? string.Empty;
        }

        return "Node " + nodeIndex;
    }

    private static string ResolveTargetSceneName(int nodeIndex)
    {
        foreach (var metadata in SceneNameJsonData.DataList)
        {
            if (metadata.OutsideScenePos == nodeIndex)
            {
                return metadata.id;
            }
        }

        return string.Empty;
    }

    private static LongLiveMapKind ResolveNodeMapKind(int nodeIndex, string targetSceneName)
    {
        if (!string.IsNullOrWhiteSpace(targetSceneName))
        {
            foreach (var metadata in SceneNameJsonData.DataList)
            {
                if (string.Equals(metadata.id, targetSceneName, StringComparison.Ordinal))
                {
                    return ResolveMapKind(metadata.MapType);
                }
            }
        }

        if (AllMapLuDainType.DataDict.TryGetValue(nodeIndex, out var nodeMeta) && nodeMeta is not null)
        {
            return nodeMeta.MapType == 1 ? LongLiveMapKind.Town : LongLiveMapKind.World;
        }

        return LongLiveMapKind.Unknown;
    }

    private static LongLiveMapKind ResolveMapKind(int mapType)
    {
        switch (mapType)
        {
            case 1:
                return LongLiveMapKind.World;
            case 2:
                return LongLiveMapKind.Town;
            case 7:
                return LongLiveMapKind.SeaIsland;
            case 20:
                return LongLiveMapKind.SeaRegion;
            case 21:
                return LongLiveMapKind.Transit;
            default:
                if (mapType >= 3 && mapType <= 19)
                {
                    return LongLiveMapKind.Dungeon;
                }

                return LongLiveMapKind.Custom;
        }
    }
}
