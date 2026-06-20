using System;
using System.Collections;
using System.Collections.Generic;
using LongLive.Mods.Maps;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveJToolsMapDraftAdapter : ILongLiveThirdPartyMapDraftAdapter
{
    private const string ModId = "tierneyjohn.jtools";
    private const string NingzhouPageId = ModId + ".page.ningzhou";
    private const string NearSeaPageId = ModId + ".page.near-sea";
    private const string FarSeaPageId = ModId + ".page.far-sea";

    public string AdapterId => "jtools.scene-metadata";

    public string SourceModId => ModId;

    public bool CanBuildDraft()
    {
        return TryResolveDataManagerInstance(out var instance)
            && CountEnumerable(ReadMemberValue(instance!, "sceneNameEntities")) > 0;
    }

    public static bool CanBuildSceneLocalTopologyBatch()
    {
        return TryResolveDataManagerInstance(out var instance)
            && CountDictionaryEntries(ReadMemberValue(instance!, "MapInfos")) > 0;
    }

    public LongLiveMapRegistryDraft BuildDraft()
    {
        var instance = ResolveDataManagerInstance();
        var sceneEntities = ResolveEnumerable(ReadMemberValue(instance, "sceneNameEntities"));

        var draft = new LongLiveMapRegistryDraft();
        EnsurePages(draft);

        foreach (var sceneEntity in sceneEntities)
        {
            TryAddSceneMetadata(draft, sceneEntity);
        }

        return draft;
    }

    public static LongLiveSceneLocalTopologyBatch BuildSceneLocalTopologyBatch()
    {
        var instance = ResolveDataManagerInstance();
        var mapInfos = ReadMemberValue(instance, "MapInfos");
        var batch = new LongLiveSceneLocalTopologyBatch();

        foreach (var mapInfoEntry in ResolveRawDictionaryEntries(mapInfos))
        {
            TryAddSceneLocalTopology(batch, mapInfoEntry.Key?.ToString(), mapInfoEntry.Value);
        }

        return batch;
    }

    private static void EnsurePages(LongLiveMapRegistryDraft draft)
    {
        draft.Pages.Add(new LongLiveWorldMapPageDescriptor
        {
            LogicalId = NingzhouPageId,
            OwningModId = ModId,
            DisplayName = "JTools Ningzhou",
            OrderHint = 7100,
        });

        draft.Pages.Add(new LongLiveWorldMapPageDescriptor
        {
            LogicalId = NearSeaPageId,
            OwningModId = ModId,
            DisplayName = "JTools Near Sea",
            OrderHint = 7200,
        });

        draft.Pages.Add(new LongLiveWorldMapPageDescriptor
        {
            LogicalId = FarSeaPageId,
            OwningModId = ModId,
            DisplayName = "JTools Far Sea",
            OrderHint = 7300,
        });
    }

    private static void TryAddSceneMetadata(LongLiveMapRegistryDraft draft, object sceneEntity)
    {
        var sceneName = ReadString(sceneEntity, "Id");
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        var displayName = ReadString(sceneEntity, "Name");
        var mapType = ReadInt(sceneEntity, "SceneType");
        var sellType = ReadInt(sceneEntity, "SellType");
        var highlightId = ReadInt(sceneEntity, "HighLightId");
        var outsideSceneName = ReadString(sceneEntity, "OutSideSceneName");
        var outsideScenePos = ReadInt(sceneEntity, "OutSideScenePosition");
        var pageId = ResolvePageId(sellType);
        var regionId = highlightId > 0 ? ModId + ".region." + highlightId : string.Empty;

        if (highlightId > 0)
        {
            EnsureRegion(draft, regionId, pageId, displayName, highlightId);
        }

        draft.Scenes.Add(new LongLiveSceneDescriptor
        {
            LogicalId = ModId + ".scene." + sceneName.ToLowerInvariant(),
            OwningModId = ModId,
            SceneName = sceneName,
            MapKind = ResolveMapKind(mapType),
            DisplayName = displayName,
            EventName = displayName,
            OverviewPageId = pageId,
            HighlightRegionId = regionId,
            OutsideSceneName = outsideSceneName,
            HostMapType = mapType > 0 ? mapType : null,
            HostHighlightId = highlightId > 0 ? highlightId : null,
            HostOutsideScenePos = outsideScenePos > 0 ? outsideScenePos : null,
        });
    }

    private static void EnsureRegion(LongLiveMapRegistryDraft draft, string regionId, string pageId, string displayName, int highlightId)
    {
        foreach (var region in draft.HighlightRegions)
        {
            if (string.Equals(region.LogicalId, regionId, StringComparison.Ordinal))
            {
                return;
            }
        }

        draft.HighlightRegions.Add(new LongLiveHighlightRegionDescriptor
        {
            LogicalId = regionId,
            OwningModId = ModId,
            PageId = pageId,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "JTools Region " + highlightId : displayName,
            HostHighlightId = highlightId,
        });
    }

    private static void TryAddSceneLocalTopology(LongLiveSceneLocalTopologyBatch batch, string? mapId, object? mapInfo)
    {
        if (string.IsNullOrWhiteSpace(mapId) || mapInfo is null)
        {
            return;
        }

        var sceneName = mapId!;
        var topologyLogicalId = ModId + ".topology." + sceneName.ToLowerInvariant();
        batch.Topologies.Add(new LongLiveSceneLocalTopologyDescriptor
        {
            LogicalId = topologyLogicalId,
            OwningModId = ModId,
            SceneLogicalId = ModId + ".scene." + sceneName.ToLowerInvariant(),
            SceneName = sceneName,
            DisplayName = ReadString(mapInfo, "Name"),
        });

        var mapNodes = ReadMemberValue(mapInfo, "MapNodes");
        var nodeLogicalIds = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

        foreach (var entry in ResolveRawDictionaryEntries(mapNodes))
        {
            if (entry.Value is null)
            {
                continue;
            }

            var nodeName = ReadString(entry.Value, "Name");
            if (string.IsNullOrWhiteSpace(nodeName))
            {
                nodeName = entry.Key?.ToString() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(nodeName))
            {
                continue;
            }

            var nodeLogicalId = topologyLogicalId + ".node." + nodeName.ToLowerInvariant();
            nodeLogicalIds[entry.Value] = nodeLogicalId;
            batch.Nodes.Add(new LongLiveSceneLocalNodeDescriptor
            {
                LogicalId = nodeLogicalId,
                TopologyLogicalId = topologyLogicalId,
                OwningModId = ModId,
                DisplayName = nodeName,
                Position = ReadVector3AsPoint(entry.Value, "Position"),
                IsCity = ReadBool(entry.Value, "IsCity"),
                IsHidden = ReadBool(entry.Value, "IsHide"),
                StaticAvatarIds = ResolveIntList(ReadMemberValue(entry.Value, "StaticAvatars")),
            });
        }

        foreach (var entry in ResolveRawDictionaryEntries(mapNodes))
        {
            if (entry.Value is null || !nodeLogicalIds.TryGetValue(entry.Value, out var nodeLogicalId))
            {
                continue;
            }

            var adjacency = ReadMemberValue(entry.Value, "AdjacentEdges");
            var connectedNodeIds = new List<string>();
            foreach (var adjacentEntry in ResolveRawDictionaryEntries(adjacency))
            {
                if (adjacentEntry.Key is null)
                {
                    continue;
                }

                if (nodeLogicalIds.TryGetValue(adjacentEntry.Key, out var connectedNodeId) && !connectedNodeIds.Contains(connectedNodeId))
                {
                    connectedNodeIds.Add(connectedNodeId);
                }
            }

            foreach (var node in batch.Nodes)
            {
                if (string.Equals(node.LogicalId, nodeLogicalId, StringComparison.Ordinal))
                {
                    node.ConnectedNodeIds = connectedNodeIds;
                    break;
                }
            }
        }
    }

    private static string ResolvePageId(int sellType)
    {
        switch (sellType)
        {
            case 2:
                return NearSeaPageId;
            case 3:
                return FarSeaPageId;
            default:
                return NingzhouPageId;
        }
    }

    private static LongLiveMapKind ResolveMapKind(int mapType)
    {
        switch (mapType)
        {
            case 1:
                return LongLiveMapKind.World;
            case 2:
                return LongLiveMapKind.Town;
            case 3:
            case 4:
            case 5:
            case 6:
            case 8:
                return LongLiveMapKind.Dungeon;
            case 7:
                return LongLiveMapKind.SeaIsland;
            case 21:
            case 22:
            case 23:
            case 24:
                return LongLiveMapKind.SeaRegion;
            default:
                return LongLiveMapKind.Custom;
        }
    }

    private static Type? ResolveDataManagerType()
    {
        return Type.GetType("TierneyJohn.MiChangSheng.JTools.Manager.DataManager, JTools");
    }

    private static bool TryResolveDataManagerInstance(out object? instance)
    {
        var dataManagerType = ResolveDataManagerType();
        if (dataManagerType is null)
        {
            instance = null;
            return false;
        }

        instance = ResolveStaticFieldValue(dataManagerType, "Inst");
        return instance is not null;
    }

    private static object ResolveDataManagerInstance()
    {
        if (!TryResolveDataManagerInstance(out var instance) || instance is null)
        {
            throw new InvalidOperationException("JTools DataManager.Inst is unavailable.");
        }

        return instance;
    }

    private static object? ResolveStaticFieldValue(Type type, string fieldName)
    {
        return type.GetField(fieldName)?.GetValue(null);
    }

    private static IEnumerable<object> ResolveEnumerable(object? value)
    {
        if (value is not IEnumerable enumerable)
        {
            yield break;
        }

        foreach (var item in enumerable)
        {
            if (item is not null)
            {
                yield return item;
            }
        }
    }

    private static IEnumerable<KeyValuePair<object?, object?>> ResolveRawDictionaryEntries(object? value)
    {
        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                yield return new KeyValuePair<object?, object?>(entry.Key, entry.Value);
            }
        }
    }

    private static int CountEnumerable(object? value)
    {
        if (value is not IEnumerable enumerable)
        {
            return 0;
        }

        var count = 0;
        foreach (var _ in enumerable)
        {
            count++;
        }

        return count;
    }

    private static int CountDictionaryEntries(object? value)
    {
        if (value is not IDictionary dictionary)
        {
            return 0;
        }

        return dictionary.Count;
    }

    private static object? ReadMemberValue(object instance, string memberName)
    {
        var type = instance.GetType();
        var property = type.GetProperty(memberName);
        if (property is not null)
        {
            return property.GetValue(instance, null);
        }

        var field = type.GetField(memberName);
        return field?.GetValue(instance);
    }

    private static string ReadString(object instance, string memberName)
    {
        return ReadMemberValue(instance, memberName)?.ToString() ?? string.Empty;
    }

    private static int ReadInt(object instance, string memberName)
    {
        var value = ReadMemberValue(instance, memberName);
        if (value is null)
        {
            return 0;
        }

        if (value is int intValue)
        {
            return intValue;
        }

        return Convert.ToInt32(value);
    }

    private static bool ReadBool(object instance, string memberName)
    {
        var value = ReadMemberValue(instance, memberName);
        if (value is null)
        {
            return false;
        }

        if (value is bool boolValue)
        {
            return boolValue;
        }

        return Convert.ToBoolean(value);
    }

    private static LongLiveMapPoint ReadVector3AsPoint(object instance, string memberName)
    {
        var value = ReadMemberValue(instance, memberName);
        if (value is null)
        {
            return new LongLiveMapPoint(0f, 0f);
        }

        var type = value.GetType();
        var xValue = type.GetProperty("x")?.GetValue(value, null) ?? type.GetField("x")?.GetValue(value);
        var yValue = type.GetProperty("y")?.GetValue(value, null) ?? type.GetField("y")?.GetValue(value);
        var x = xValue is null ? 0f : Convert.ToSingle(xValue);
        var y = yValue is null ? 0f : Convert.ToSingle(yValue);
        return new LongLiveMapPoint(x, y);
    }

    private static List<int> ResolveIntList(object? value)
    {
        var result = new List<int>();
        if (value is not IEnumerable enumerable)
        {
            return result;
        }

        foreach (var item in enumerable)
        {
            if (item is null)
            {
                continue;
            }

            result.Add(Convert.ToInt32(item));
        }

        return result;
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();

        public new bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
