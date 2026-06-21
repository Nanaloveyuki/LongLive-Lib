using System;
using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveCustomMapRuntimeBootstrapCatalog : ILongLiveCustomMapRuntimeBootstrapCatalog
{
    private readonly Dictionary<string, LongLiveCustomMapRuntimeBootstrapDescriptor> _bySceneLogicalId = new Dictionary<string, LongLiveCustomMapRuntimeBootstrapDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveCustomMapRuntimeBootstrapDescriptor> _bySceneName = new Dictionary<string, LongLiveCustomMapRuntimeBootstrapDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveCustomMapRuntimeBootstrapDescriptor> _byEntryNodeLogicalId = new Dictionary<string, LongLiveCustomMapRuntimeBootstrapDescriptor>(StringComparer.Ordinal);

    public IReadOnlyCollection<LongLiveCustomMapRuntimeBootstrapDescriptor> Bootstraps => _bySceneLogicalId.Values;

    public int BootstrapCount => _bySceneLogicalId.Count;

    public void Register(LongLiveCustomMapRuntimeBootstrapDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.SceneLogicalId))
        {
            throw new ArgumentException("Custom map runtime bootstrap descriptor must define a scene logical ID.", nameof(descriptor));
        }

        _bySceneLogicalId[descriptor.SceneLogicalId] = descriptor;

        if (!string.IsNullOrWhiteSpace(descriptor.SceneName))
        {
            _bySceneName[descriptor.SceneName] = descriptor;
        }

        if (!string.IsNullOrWhiteSpace(descriptor.EntryNodeLogicalId))
        {
            _byEntryNodeLogicalId[descriptor.EntryNodeLogicalId] = descriptor;
        }
    }

    public bool TryGetBySceneLogicalId(string sceneLogicalId, out LongLiveCustomMapRuntimeBootstrapDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(sceneLogicalId))
        {
            descriptor = null;
            return false;
        }

        return _bySceneLogicalId.TryGetValue(sceneLogicalId, out descriptor);
    }

    public bool TryGetBySceneName(string sceneName, out LongLiveCustomMapRuntimeBootstrapDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            descriptor = null;
            return false;
        }

        return _bySceneName.TryGetValue(sceneName, out descriptor);
    }

    public bool TryGetByEntryNodeLogicalId(string nodeLogicalId, out LongLiveCustomMapRuntimeBootstrapDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(nodeLogicalId))
        {
            descriptor = null;
            return false;
        }

        return _byEntryNodeLogicalId.TryGetValue(nodeLogicalId, out descriptor);
    }

    public IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByOwningModId(string owningModId)
    {
        return Filter(descriptor => string.Equals(descriptor.OwningModId, owningModId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByOverviewPageId(string pageId)
    {
        return Filter(descriptor => string.Equals(descriptor.OverviewPageId, pageId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByHighlightRegionId(string regionId)
    {
        return Filter(descriptor => string.Equals(descriptor.HighlightRegionId, regionId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByReturnSceneLogicalId(string sceneLogicalId)
    {
        return Filter(descriptor => string.Equals(descriptor.ReturnSceneLogicalId, sceneLogicalId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByReturnSceneName(string sceneName)
    {
        return Filter(descriptor => string.Equals(descriptor.ReturnSceneName, sceneName, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByRouteKind(LongLive.Mods.SceneRouting.LongLiveSceneRouteKind routeKind)
    {
        return Filter(descriptor => descriptor.RouteKind == routeKind);
    }

    private IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> Filter(Func<LongLiveCustomMapRuntimeBootstrapDescriptor, bool> predicate)
    {
        var results = new List<LongLiveCustomMapRuntimeBootstrapDescriptor>();
        foreach (var descriptor in _bySceneLogicalId.Values)
        {
            if (predicate(descriptor))
            {
                results.Add(descriptor);
            }
        }

        return results;
    }
}
