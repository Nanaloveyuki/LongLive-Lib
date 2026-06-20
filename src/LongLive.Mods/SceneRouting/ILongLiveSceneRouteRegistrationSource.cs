using LongLive.Mods.Maps;

namespace LongLive.Mods.SceneRouting;

public interface ILongLiveSceneRouteRegistrationSource
{
    string Name { get; }

    LongLiveMapRegistryPlan CreatePlan();
}
