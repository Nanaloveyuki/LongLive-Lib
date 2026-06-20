using System;
using LongLive.Mods.Maps;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveHostMapSnapshotRouteSource : ILongLiveSceneRouteRegistrationSource
{
    public string Name => "host-map-snapshot";

    public LongLiveMapRegistryPlan CreatePlan()
    {
        var draft = new LongLiveMapSnapshotAdapter().CaptureCurrentSnapshot();
        return new LongLiveMapRegistryPlanner().CreatePlan(draft);
    }
}
