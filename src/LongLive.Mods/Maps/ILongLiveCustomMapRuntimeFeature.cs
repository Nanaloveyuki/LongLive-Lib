using LongLive.Mods.SceneRouting;

namespace LongLive.Mods.Maps;

public interface ILongLiveCustomMapRuntimeFeature : ILongLiveMapRegistryFeature
{
    ILongLiveCustomMapRuntimeCatalog Catalog { get; }

    ILongLiveCustomMapRuntimeBootstrapCatalog Bootstraps { get; }

    ILongLiveSceneLocalTopologyCatalog SceneLocalTopologies { get; }
}
