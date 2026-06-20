using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public interface ILongLiveCustomMapRuntimeCatalog
{
    IReadOnlyCollection<LongLiveSceneDescriptor> Scenes { get; }

    int SceneCount { get; }

    bool TryGetByLogicalId(string logicalId, out LongLiveSceneDescriptor? descriptor);

    bool TryGetBySceneName(string sceneName, out LongLiveSceneDescriptor? descriptor);

    IReadOnlyList<LongLiveSceneDescriptor> GetScenesForMod(string owningModId);
}
