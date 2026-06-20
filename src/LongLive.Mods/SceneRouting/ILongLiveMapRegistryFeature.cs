using LongLive.Mods.Maps;

namespace LongLive.Mods.SceneRouting;

public interface ILongLiveMapRegistryFeature : ILongLiveSceneRoutingFeature
{
    void RegisterPlan(LongLiveMapRegistryPlan plan);
}
