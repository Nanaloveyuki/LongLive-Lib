namespace LongLive.Mods.SceneRouting;

public interface ILongLiveSceneRouteResolver
{
    bool TryResolveBySceneName(string sceneName, out LongLiveSceneRouteDescriptor? descriptor);

    bool TryResolveByLogicalId(string logicalId, out LongLiveSceneRouteDescriptor? descriptor);

    LongLiveSceneRouteKind ResolveSceneKind(string sceneName);
}
