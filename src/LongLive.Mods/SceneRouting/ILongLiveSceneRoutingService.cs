namespace LongLive.Mods.SceneRouting;

public interface ILongLiveSceneRoutingService : ILongLiveSceneRouteResolver, ILongLiveSceneRoutingRegistrationSink
{
    LongLiveSceneRouteCatalog Catalog { get; }

    LongLiveSceneRouteResolution Resolve(LongLiveSceneAddress address);

    LongLiveSceneRoutingSnapshot CaptureSnapshot();

    LongLiveSceneRouteResult WarpPlayer(LongLiveSceneAddress address);

    LongLiveSceneRouteResult WarpNpc(int npcId, LongLiveSceneAddress address);
}
