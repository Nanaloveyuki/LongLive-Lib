using System;
using System.Collections.Generic;
using LongLive.Mods.SceneRouting;

namespace LongLive.Mods.Maps;

public sealed class LongLiveMapOverviewRoutingProjectionCatalog : ILongLiveMapOverviewRoutingProjectionCatalog
{
    private readonly Dictionary<string, LongLiveMapOverviewRouteProjection> _byNodeId = new Dictionary<string, LongLiveMapOverviewRouteProjection>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveMapOverviewRouteProjection> _bySceneLogicalId = new Dictionary<string, LongLiveMapOverviewRouteProjection>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveMapOverviewRouteProjection> _bySceneName = new Dictionary<string, LongLiveMapOverviewRouteProjection>(StringComparer.Ordinal);

    public IReadOnlyCollection<LongLiveMapOverviewRouteProjection> Projections => _byNodeId.Values;

    public int ProjectionCount => _byNodeId.Count;

    public void Register(LongLiveMapOverviewRouteProjection projection)
    {
        if (projection is null)
        {
            throw new ArgumentNullException(nameof(projection));
        }

        if (string.IsNullOrWhiteSpace(projection.NodeLogicalId))
        {
            throw new ArgumentException("Map overview route projection must define a node logical ID.", nameof(projection));
        }

        _byNodeId[projection.NodeLogicalId] = projection;

        if (!string.IsNullOrWhiteSpace(projection.SceneLogicalId))
        {
            _bySceneLogicalId[projection.SceneLogicalId] = projection;
        }

        if (!string.IsNullOrWhiteSpace(projection.SceneName))
        {
            _bySceneName[projection.SceneName] = projection;
        }
    }

    public bool TryGetByNodeId(string nodeLogicalId, out LongLiveMapOverviewRouteProjection? projection)
    {
        if (string.IsNullOrWhiteSpace(nodeLogicalId))
        {
            projection = null;
            return false;
        }

        return _byNodeId.TryGetValue(nodeLogicalId, out projection);
    }

    public bool TryGetBySceneLogicalId(string sceneLogicalId, out LongLiveMapOverviewRouteProjection? projection)
    {
        if (string.IsNullOrWhiteSpace(sceneLogicalId))
        {
            projection = null;
            return false;
        }

        return _bySceneLogicalId.TryGetValue(sceneLogicalId, out projection);
    }

    public bool TryGetBySceneName(string sceneName, out LongLiveMapOverviewRouteProjection? projection)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            projection = null;
            return false;
        }

        return _bySceneName.TryGetValue(sceneName, out projection);
    }

    public IReadOnlyList<LongLiveMapOverviewRouteProjection> GetByPageId(string pageId)
    {
        return Filter(projection => string.Equals(projection.PageId, pageId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveMapOverviewRouteProjection> GetByRegionId(string regionId)
    {
        return Filter(projection => string.Equals(projection.RegionId, regionId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveMapOverviewRouteProjection> GetByOwningModId(string owningModId)
    {
        return Filter(projection => string.Equals(projection.OwningModId, owningModId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveMapOverviewRouteProjection> GetByRouteKind(LongLiveSceneRouteKind routeKind)
    {
        return Filter(projection => projection.RouteKind == routeKind);
    }

    public bool TryCreateSceneAddressForNode(string nodeLogicalId, out LongLiveSceneAddress? address)
    {
        if (TryGetByNodeId(nodeLogicalId, out var projection) && projection is not null)
        {
            address = projection.CreateSceneAddress();
            return true;
        }

        address = null;
        return false;
    }

    private IReadOnlyList<LongLiveMapOverviewRouteProjection> Filter(Func<LongLiveMapOverviewRouteProjection, bool> predicate)
    {
        var results = new List<LongLiveMapOverviewRouteProjection>();
        foreach (var projection in _byNodeId.Values)
        {
            if (predicate(projection))
            {
                results.Add(projection);
            }
        }

        return results;
    }
}
