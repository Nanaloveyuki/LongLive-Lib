using System;
using System.Collections.Generic;

namespace LongLive.Mods.SceneRouting;

public sealed class LongLiveSceneRouteCatalog
{
    private readonly Dictionary<string, LongLiveSceneRouteDescriptor> _bySceneName = new Dictionary<string, LongLiveSceneRouteDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveSceneRouteDescriptor> _byLogicalId = new Dictionary<string, LongLiveSceneRouteDescriptor>(StringComparer.Ordinal);

    public IReadOnlyCollection<LongLiveSceneRouteDescriptor> Routes => _bySceneName.Values;

    public void Register(LongLiveSceneRouteDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.SceneName))
        {
            throw new ArgumentException("Scene route descriptor must define a host scene name.", nameof(descriptor));
        }

        _bySceneName[descriptor.SceneName] = descriptor;

        if (!string.IsNullOrWhiteSpace(descriptor.LogicalId))
        {
            _byLogicalId[descriptor.LogicalId] = descriptor;
        }
    }

    public bool TryGetBySceneName(string sceneName, out LongLiveSceneRouteDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            descriptor = null;
            return false;
        }

        return _bySceneName.TryGetValue(sceneName, out descriptor);
    }

    public bool TryGetByLogicalId(string logicalId, out LongLiveSceneRouteDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(logicalId))
        {
            descriptor = null;
            return false;
        }

        return _byLogicalId.TryGetValue(logicalId, out descriptor);
    }
}
