using System;
using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveCustomMapRuntimeCatalog : ILongLiveCustomMapRuntimeCatalog
{
    private readonly Dictionary<string, LongLiveSceneDescriptor> _scenes = new Dictionary<string, LongLiveSceneDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveSceneDescriptor> _scenesByName = new Dictionary<string, LongLiveSceneDescriptor>(StringComparer.Ordinal);

    public IReadOnlyCollection<LongLiveSceneDescriptor> Scenes => _scenes.Values;

    public int SceneCount => _scenes.Count;

    public void RegisterScene(LongLiveSceneDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        _scenes[descriptor.LogicalId] = descriptor;
        if (!string.IsNullOrWhiteSpace(descriptor.SceneName))
        {
            _scenesByName[descriptor.SceneName] = descriptor;
        }
    }

    public bool TryGetByLogicalId(string logicalId, out LongLiveSceneDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(logicalId))
        {
            descriptor = null;
            return false;
        }

        return _scenes.TryGetValue(logicalId, out descriptor);
    }

    public bool TryGetBySceneName(string sceneName, out LongLiveSceneDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            descriptor = null;
            return false;
        }

        return _scenesByName.TryGetValue(sceneName, out descriptor);
    }

    public IReadOnlyList<LongLiveSceneDescriptor> GetScenesForMod(string owningModId)
    {
        return Filter(scene => string.Equals(scene.OwningModId, owningModId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveSceneDescriptor> GetScenesForOverviewPageId(string pageId)
    {
        return Filter(scene => string.Equals(scene.OverviewPageId, pageId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveSceneDescriptor> GetScenesForHighlightRegionId(string regionId)
    {
        return Filter(scene => string.Equals(scene.HighlightRegionId, regionId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveSceneDescriptor> GetScenesForMapKind(LongLiveMapKind mapKind)
    {
        return Filter(scene => scene.MapKind == mapKind);
    }

    private IReadOnlyList<LongLiveSceneDescriptor> Filter(Func<LongLiveSceneDescriptor, bool> predicate)
    {
        var results = new List<LongLiveSceneDescriptor>();
        foreach (var scene in _scenes.Values)
        {
            if (predicate(scene))
            {
                results.Add(scene);
            }
        }

        return results;
    }
}
