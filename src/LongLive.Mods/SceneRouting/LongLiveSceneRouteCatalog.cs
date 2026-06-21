using System;
using System.Collections.Generic;

namespace LongLive.Mods.SceneRouting;

public sealed class LongLiveSceneRouteCatalog
{
    private readonly Dictionary<string, LongLiveSceneRouteDescriptor> _bySceneName = new Dictionary<string, LongLiveSceneRouteDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveSceneRouteDescriptor> _byLogicalId = new Dictionary<string, LongLiveSceneRouteDescriptor>(StringComparer.Ordinal);

    public IReadOnlyCollection<LongLiveSceneRouteDescriptor> Routes => _bySceneName.Values;

    public int RouteCount => _bySceneName.Count;

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

    public IReadOnlyList<LongLiveSceneRouteDescriptor> GetByOwningModId(string owningModId)
    {
        return Filter(route => string.Equals(route.OwningModId, owningModId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveSceneRouteDescriptor> GetByOverviewPageId(string pageId)
    {
        return Filter(route => string.Equals(route.OverviewPageId, pageId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveSceneRouteDescriptor> GetByHighlightRegionId(string regionId)
    {
        return Filter(route => string.Equals(route.HighlightRegionId, regionId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveSceneRouteDescriptor> GetByRouteKind(LongLiveSceneRouteKind routeKind)
    {
        return Filter(route => route.RouteKind == routeKind);
    }

    private IReadOnlyList<LongLiveSceneRouteDescriptor> Filter(Func<LongLiveSceneRouteDescriptor, bool> predicate)
    {
        var results = new List<LongLiveSceneRouteDescriptor>();
        foreach (var descriptor in _bySceneName.Values)
        {
            if (predicate(descriptor))
            {
                results.Add(descriptor);
            }
        }

        return results;
    }
}
