namespace LongLive.Mods.SceneRouting;

public interface ILongLiveSceneRoutingFeature
{
    string Name { get; }

    void Initialize(ILongLiveSceneRoutingService sceneRouting);
}
