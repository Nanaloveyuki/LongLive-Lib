using LongLive.Mods.SceneRouting;

namespace LongLive.Mods.Maps;

public interface ILongLiveMapOverviewFeature : ILongLiveMapRegistryFeature
{
    ILongLiveMapOverviewCatalog Catalog { get; }

    ILongLiveMapOverviewRoutingProjectionCatalog Routing { get; }
}
