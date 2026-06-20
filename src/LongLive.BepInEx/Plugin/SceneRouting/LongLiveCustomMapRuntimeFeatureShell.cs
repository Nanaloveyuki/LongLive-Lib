using System;
using LongLive.Mods.Maps;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeFeatureShell : ILongLiveCustomMapRuntimeFeature
{
    private readonly LongLiveCustomMapRuntimeRegistry _registry = new LongLiveCustomMapRuntimeRegistry();

    public string Name => "custom-map-runtime-shell";

    public ILongLiveCustomMapRuntimeCatalog Catalog => _registry.Catalog;

    public ILongLiveCustomMapRuntimeBootstrapCatalog Bootstraps => _registry.Bootstraps;

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
