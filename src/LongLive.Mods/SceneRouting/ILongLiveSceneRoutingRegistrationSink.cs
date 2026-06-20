using LongLive.Mods.Maps;

namespace LongLive.Mods.SceneRouting;

public interface ILongLiveSceneRoutingRegistrationSink
{
    void RegisterPlan(LongLiveMapRegistryPlan plan);

    void RegisterSource(ILongLiveSceneRouteRegistrationSource source);
}
