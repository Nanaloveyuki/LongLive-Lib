using System;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveSceneRoutingFeatureShell : ILongLiveSceneRoutingFeature
{
    public string Name => "scene-routing-shell";

    public void Initialize(ILongLiveSceneRoutingService sceneRouting)
    {
        if (sceneRouting is null)
        {
            throw new ArgumentNullException(nameof(sceneRouting));
        }

        _ = sceneRouting.Catalog;
    }
}
