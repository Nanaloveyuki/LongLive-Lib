using LongLive.Mods.Maps;

namespace LongLive.Mods.SceneRouting;

public static class LongLiveSceneRoutingMapper
{
    public static LongLiveSceneRouteKind ToRouteKind(LongLiveMapKind mapKind)
    {
        switch (mapKind)
        {
            case LongLiveMapKind.World:
                return LongLiveSceneRouteKind.WorldMap;
            case LongLiveMapKind.Town:
                return LongLiveSceneRouteKind.RegionScene;
            case LongLiveMapKind.SeaRegion:
            case LongLiveMapKind.SeaIsland:
                return LongLiveSceneRouteKind.SeaScene;
            case LongLiveMapKind.Dungeon:
                return LongLiveSceneRouteKind.DungeonScene;
            default:
                return LongLiveSceneRouteKind.Unknown;
        }
    }
}
