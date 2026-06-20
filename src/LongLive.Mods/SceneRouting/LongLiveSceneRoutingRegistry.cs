using System;
using LongLive.Mods.Maps;

namespace LongLive.Mods.SceneRouting;

public sealed class LongLiveSceneRoutingRegistry
{
    private readonly LongLiveSceneRouteCatalog _catalog = new LongLiveSceneRouteCatalog();

    public LongLiveSceneRouteCatalog Catalog => _catalog;

    public void RegisterPlan(LongLiveMapRegistryPlan plan)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (!plan.Validation.IsValid)
        {
            throw new InvalidOperationException("Cannot register an invalid map registry plan into scene routing.");
        }

        foreach (var scene in plan.Draft.Scenes)
        {
            _catalog.Register(new LongLiveSceneRouteDescriptor
            {
                LogicalId = scene.LogicalId,
                SceneName = scene.SceneName,
                RouteKind = LongLiveSceneRoutingMapper.ToRouteKind(scene.MapKind),
                DisplayName = scene.DisplayName,
                HostMapType = scene.HostMapType,
                HostOutsideScenePos = scene.HostOutsideScenePos,
                OutsideSceneName = scene.OutsideSceneName,
                OutsideSceneLogicalId = scene.OutsideSceneLogicalId,
            });
        }
    }
}
