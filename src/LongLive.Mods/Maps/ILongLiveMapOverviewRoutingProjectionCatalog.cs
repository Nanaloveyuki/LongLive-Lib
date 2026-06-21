using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public interface ILongLiveMapOverviewRoutingProjectionCatalog
{
    IReadOnlyCollection<LongLiveMapOverviewRouteProjection> Projections { get; }

    int ProjectionCount { get; }

    bool TryGetByNodeId(string nodeLogicalId, out LongLiveMapOverviewRouteProjection? projection);

    bool TryGetBySceneLogicalId(string sceneLogicalId, out LongLiveMapOverviewRouteProjection? projection);

    bool TryGetBySceneName(string sceneName, out LongLiveMapOverviewRouteProjection? projection);

    IReadOnlyList<LongLiveMapOverviewRouteProjection> GetByPageId(string pageId);

    IReadOnlyList<LongLiveMapOverviewRouteProjection> GetByRegionId(string regionId);

    IReadOnlyList<LongLiveMapOverviewRouteProjection> GetByOwningModId(string owningModId);

    IReadOnlyList<LongLiveMapOverviewRouteProjection> GetByRouteKind(LongLive.Mods.SceneRouting.LongLiveSceneRouteKind routeKind);

    bool TryCreateSceneAddressForNode(string nodeLogicalId, out LongLive.Mods.SceneRouting.LongLiveSceneAddress? address);
}
