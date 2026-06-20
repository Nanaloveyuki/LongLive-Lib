using System;
using BepInEx.Logging;
using LongLive.Mods.Maps;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveSceneRoutingHost
{
    private readonly LongLiveSceneRoutingFeatureCollection _features;
    private readonly LongLiveSceneRoutingCoordinator _coordinator;
    private readonly LongLiveSceneRoutingFeatureShell _sceneRoutingFeature;
    private readonly LongLiveMapOverviewFeatureShell _mapOverviewFeature;
    private readonly LongLiveCustomMapRuntimeFeatureShell _customMapRuntimeFeature;

    public LongLiveSceneRoutingHost(ManualLogSource logger, ILongLiveSceneRoutingService sceneRouting)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (sceneRouting is null)
        {
            throw new ArgumentNullException(nameof(sceneRouting));
        }

        _features = new LongLiveSceneRoutingFeatureCollection();
        _sceneRoutingFeature = new LongLiveSceneRoutingFeatureShell();
        _mapOverviewFeature = new LongLiveMapOverviewFeatureShell();
        _customMapRuntimeFeature = new LongLiveCustomMapRuntimeFeatureShell();
        _features.Add(_sceneRoutingFeature);
        _features.Add(_mapOverviewFeature);
        _features.Add(_customMapRuntimeFeature);
        _coordinator = new LongLiveSceneRoutingCoordinator(logger, sceneRouting, _features);
    }

    public LongLiveSceneRoutingFeatureCollection Features => _features;

    public LongLiveSceneRoutingCoordinator Coordinator => _coordinator;

    public ILongLiveMapOverviewFeature MapOverview => _mapOverviewFeature;

    public ILongLiveCustomMapRuntimeFeature CustomMapRuntime => _customMapRuntimeFeature;

    public void RegisterFeature(ILongLiveSceneRoutingFeature feature)
    {
        _features.Add(feature);
    }

    public void InitializeFeatures()
    {
        _coordinator.InitializeFeatures();
    }

    public void RegisterSource(ILongLiveSceneRouteRegistrationSource source)
    {
        _coordinator.RegisterSource(source);
    }

    public void RegisterPlan(LongLiveMapRegistryPlan plan, string sourceName)
    {
        _coordinator.RegisterPlan(plan, sourceName);
    }
}
