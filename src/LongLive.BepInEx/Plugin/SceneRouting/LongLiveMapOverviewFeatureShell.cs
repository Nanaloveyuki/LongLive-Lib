using System;
using LongLive.Mods.Maps;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewFeatureShell : ILongLiveMapOverviewFeature
{
    private readonly LongLiveMapOverviewRegistry _registry = new LongLiveMapOverviewRegistry();

    public string Name => "map-overview-shell";

    public ILongLiveMapOverviewCatalog Catalog => _registry.Catalog;

    public ILongLiveMapOverviewRoutingProjectionCatalog Routing => _registry.Routing;

    public void RegisterPlan(LongLiveMapRegistryPlan plan)
    {
        _registry.RegisterPlan(plan);
    }

    public void Initialize(ILongLiveSceneRoutingService sceneRouting)
    {
        if (sceneRouting is null)
        {
            throw new ArgumentNullException(nameof(sceneRouting));
        }
    }
}
