using System;
using System.Collections.Generic;
using LongLive.Mods.Maps;

namespace LongLive.Mods.SceneRouting;

public sealed class LongLiveSceneRoutingFeatureCollection
{
    private readonly List<ILongLiveSceneRoutingFeature> _features = new List<ILongLiveSceneRoutingFeature>();
    private readonly HashSet<ILongLiveSceneRoutingFeature> _initializedFeatures = new HashSet<ILongLiveSceneRoutingFeature>();
    private ILongLiveSceneRoutingService? _sceneRouting;

    public IReadOnlyList<ILongLiveSceneRoutingFeature> Features => _features;

    public void Add(ILongLiveSceneRoutingFeature feature)
    {
        if (feature is null)
        {
            throw new ArgumentNullException(nameof(feature));
        }

        if (_features.Contains(feature))
        {
            return;
        }

        _features.Add(feature);

        if (_sceneRouting is not null)
        {
            InitializeFeature(feature, _sceneRouting);
        }
    }

    public void InitializeAll(ILongLiveSceneRoutingService sceneRouting)
    {
        if (sceneRouting is null)
        {
            throw new ArgumentNullException(nameof(sceneRouting));
        }

        _sceneRouting = sceneRouting;

        foreach (var feature in _features)
        {
            InitializeFeature(feature, sceneRouting);
        }
    }

    public bool TryGet<TFeature>(out TFeature? feature)
        where TFeature : class, ILongLiveSceneRoutingFeature
    {
        foreach (var candidate in _features)
        {
            if (candidate is TFeature typed)
            {
                feature = typed;
                return true;
            }
        }

        feature = null;
        return false;
    }

    public void RegisterPlanAcrossFeatures(LongLiveMapRegistryPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        foreach (var feature in _features)
        {
            if (feature is ILongLiveMapRegistryFeature registryFeature)
            {
                registryFeature.RegisterPlan(plan);
            }
        }
    }

    private void InitializeFeature(ILongLiveSceneRoutingFeature feature, ILongLiveSceneRoutingService sceneRouting)
    {
        if (_initializedFeatures.Contains(feature))
        {
            return;
        }

        feature.Initialize(sceneRouting);
        _initializedFeatures.Add(feature);
    }
}
