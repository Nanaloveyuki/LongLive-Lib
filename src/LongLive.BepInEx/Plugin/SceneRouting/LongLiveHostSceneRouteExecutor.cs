using System;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveHostSceneRouteExecutor
{
    private readonly LongLiveBepInExSceneRoutingService _routing;

    public LongLiveHostSceneRouteExecutor(LongLiveBepInExSceneRoutingService routing)
    {
        _routing = routing ?? throw new ArgumentNullException(nameof(routing));
    }

    public LongLiveSceneRouteResult WarpPlayer(LongLiveSceneAddress address, LongLiveSceneRouteResolution resolution)
    {
        return _routing.ExecutePlayerWarp(address, resolution);
    }

    public LongLiveSceneRouteResult WarpNpc(int npcId, LongLiveSceneAddress address, LongLiveSceneRouteResolution resolution)
    {
        return _routing.ExecuteNpcWarp(npcId, address, resolution);
    }
}
