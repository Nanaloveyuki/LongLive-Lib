using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public interface ILongLiveCustomMapRuntimeBootstrapCatalog
{
    IReadOnlyCollection<LongLiveCustomMapRuntimeBootstrapDescriptor> Bootstraps { get; }

    int BootstrapCount { get; }

    bool TryGetBySceneLogicalId(string sceneLogicalId, out LongLiveCustomMapRuntimeBootstrapDescriptor? descriptor);

    bool TryGetBySceneName(string sceneName, out LongLiveCustomMapRuntimeBootstrapDescriptor? descriptor);

    bool TryGetByEntryNodeLogicalId(string nodeLogicalId, out LongLiveCustomMapRuntimeBootstrapDescriptor? descriptor);

    IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByOwningModId(string owningModId);

    IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByOverviewPageId(string pageId);

    IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByHighlightRegionId(string regionId);

    IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByReturnSceneLogicalId(string sceneLogicalId);

    IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByReturnSceneName(string sceneName);

    IReadOnlyList<LongLiveCustomMapRuntimeBootstrapDescriptor> GetByRouteKind(LongLive.Mods.SceneRouting.LongLiveSceneRouteKind routeKind);
}
