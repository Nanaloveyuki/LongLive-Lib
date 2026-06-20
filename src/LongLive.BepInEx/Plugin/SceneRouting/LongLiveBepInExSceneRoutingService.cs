using System;
using BepInEx.Logging;
using JSONClass;
using LongLive.Mods.Maps;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveBepInExSceneRoutingService : ILongLiveSceneRoutingService
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveSceneRoutingRegistry _registry;
    private readonly LongLiveHostSceneRouteExecutor _executor;

    public LongLiveBepInExSceneRoutingService(ManualLogSource logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registry = new LongLiveSceneRoutingRegistry();
        _executor = new LongLiveHostSceneRouteExecutor(this);
    }

    public LongLiveSceneRouteCatalog Catalog => _registry.Catalog;

    public void RegisterPlan(LongLiveMapRegistryPlan plan)
    {
        _registry.RegisterPlan(plan);
    }

    public void RegisterSource(ILongLiveSceneRouteRegistrationSource source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        RegisterPlan(source.CreatePlan());
    }

    public bool TryResolveBySceneName(string sceneName, out LongLiveSceneRouteDescriptor? descriptor)
    {
        return Catalog.TryGetBySceneName(sceneName, out descriptor);
    }

    public bool TryResolveByLogicalId(string logicalId, out LongLiveSceneRouteDescriptor? descriptor)
    {
        return Catalog.TryGetByLogicalId(logicalId, out descriptor);
    }

    public LongLiveSceneRouteResolution Resolve(LongLiveSceneAddress address)
    {
        if (address is null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        var resolution = new LongLiveSceneRouteResolution
        {
            RequestedSceneName = address.SceneName,
            RequestedLogicalId = address.LogicalSceneId,
        };

        LongLiveSceneRouteDescriptor? descriptor = null;

        if (!string.IsNullOrWhiteSpace(address.LogicalSceneId))
        {
            TryResolveByLogicalId(address.LogicalSceneId, out descriptor);
        }

        if (descriptor is null && !string.IsNullOrWhiteSpace(address.SceneName))
        {
            TryResolveBySceneName(address.SceneName, out descriptor);
        }

        resolution.Descriptor = descriptor;
        resolution.RouteKind = descriptor?.RouteKind ?? ResolveSceneKind(address.SceneName);
        return resolution;
    }

    public LongLiveSceneRouteKind ResolveSceneKind(string sceneName)
    {
        if (Catalog.TryGetBySceneName(sceneName, out var registered) && registered is not null)
        {
            return registered.RouteKind;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return LongLiveSceneRouteKind.Unknown;
        }

        if (string.Equals(sceneName, "AllMaps", StringComparison.OrdinalIgnoreCase))
        {
            return LongLiveSceneRouteKind.WorldMap;
        }

        if (string.Equals(sceneName, "FRandomBase", StringComparison.OrdinalIgnoreCase))
        {
            return LongLiveSceneRouteKind.RandomDungeonScene;
        }

        if (sceneName.StartsWith("Sea", StringComparison.OrdinalIgnoreCase))
        {
            return LongLiveSceneRouteKind.SeaScene;
        }

        if (sceneName.StartsWith("F", StringComparison.OrdinalIgnoreCase))
        {
            return LongLiveSceneRouteKind.DungeonScene;
        }

        if (sceneName.StartsWith("S", StringComparison.OrdinalIgnoreCase))
        {
            return LongLiveSceneRouteKind.RegionScene;
        }

        return TryResolveSceneKindFromMetadata(sceneName);
    }

    public LongLiveSceneRoutingSnapshot CaptureSnapshot()
    {
        var sceneName = SafeGetCurrentSceneName();
        var snapshot = new LongLiveSceneRoutingSnapshot
        {
            ActiveSceneName = sceneName,
            ActiveSceneKind = ResolveSceneKind(sceneName),
            PlaceName = ResolvePlaceName(sceneName),
        };

        try
        {
            if (PlayerEx.Player is null)
            {
                return snapshot;
            }

            snapshot.PlayerNowMapIndex = PlayerEx.Player.NowMapIndex;
            snapshot.PlayerLastScene = PlayerEx.Player.lastScence ?? string.Empty;
            snapshot.PlayerLastFuBenScene = PlayerEx.Player.lastFuBenScence ?? string.Empty;
            snapshot.PlayerNowFuBen = PlayerEx.Player.NowFuBen ?? string.Empty;

            if (snapshot.ActiveSceneKind == LongLiveSceneRouteKind.DungeonScene
                || snapshot.ActiveSceneKind == LongLiveSceneRouteKind.RandomDungeonScene
                || snapshot.ActiveSceneKind == LongLiveSceneRouteKind.SeaScene)
            {
                snapshot.CurrentFuBenIndex = TryResolveCurrentFuBenIndex(sceneName);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning($"LongLive scene-routing snapshot capture encountered an error: {exception.GetType().Name}: {exception.Message}");
        }

        return snapshot;
    }

    public LongLiveSceneRouteResult WarpPlayer(LongLiveSceneAddress address)
    {
        if (address is null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        var resolution = Resolve(address);
        var routeKind = resolution.RouteKind;

        if (Tools.instance is null || PlayerEx.Player is null)
        {
            return LongLiveSceneRouteResult.Failure(
                address.SceneName,
                routeKind,
                address.EntryIndex,
                "host-unavailable",
                "Player or Tools host runtime is unavailable.");
        }

        if (routeKind == LongLiveSceneRouteKind.Unknown)
        {
            return LongLiveSceneRouteResult.Failure(
                address.SceneName,
                routeKind,
                address.EntryIndex,
                "unknown-scene-kind",
                "Unable to resolve route kind from the requested scene name.");
        }

        return _executor.WarpPlayer(address, resolution);
    }

    public LongLiveSceneRouteResult WarpNpc(int npcId, LongLiveSceneAddress address)
    {
        if (address is null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        var resolution = Resolve(address);
        var routeKind = resolution.RouteKind;

        var normalizedNpcId = NPCEx.NPCIDToNew(npcId);
        if (NPCEx.IsDeath(normalizedNpcId))
        {
            return LongLiveSceneRouteResult.Failure(
                address.SceneName,
                routeKind,
                address.EntryIndex,
                "npc-dead",
                "The target NPC is dead and cannot be routed.");
        }

        if (routeKind == LongLiveSceneRouteKind.Unknown)
        {
            return LongLiveSceneRouteResult.Failure(
                address.SceneName,
                routeKind,
                address.EntryIndex,
                "unknown-scene-kind",
                "Unable to resolve route kind from the requested scene name.");
        }

        return _executor.WarpNpc(normalizedNpcId, address, resolution);
    }

    private static LongLiveSceneRouteKind TryResolveSceneKindFromMetadata(string sceneName)
    {
        try
        {
            if (SceneNameJsonData.DataDict.TryGetValue(sceneName, out var metadata) && metadata is not null)
            {
                switch (metadata.MoneyType)
                {
                    case 1:
                        return LongLiveSceneRouteKind.RegionScene;
                    case 2:
                    case 3:
                        return LongLiveSceneRouteKind.SeaScene;
                }
            }
        }
        catch
        {
        }

        return LongLiveSceneRouteKind.Unknown;
    }

    private static string SafeGetCurrentSceneName()
    {
        try
        {
            return SceneEx.NowSceneName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private string ResolvePlaceName(string sceneName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return string.Empty;
            }

            if (Catalog.TryGetBySceneName(sceneName, out var registered) && registered is not null && !string.IsNullOrWhiteSpace(registered.DisplayName))
            {
                return registered.DisplayName;
            }

            if (PlayerEx.Player is not null && string.Equals(sceneName, "FRandomBase", StringComparison.OrdinalIgnoreCase))
            {
                return PlayerEx.Player.lastFuBenScence ?? string.Empty;
            }

            if (SceneNameJsonData.DataDict.TryGetValue(sceneName, out var metadata) && metadata is not null)
            {
                return metadata.MapName ?? metadata.EventName ?? sceneName;
            }
        }
        catch
        {
        }

        return sceneName;
    }

    private static int? TryResolveCurrentFuBenIndex(string sceneName)
    {
        try
        {
            if (PlayerEx.Player is null || string.IsNullOrWhiteSpace(sceneName))
            {
                return null;
            }

            if (PlayerEx.Player.fubenContorl is null)
            {
                return null;
            }

            return PlayerEx.Player.fubenContorl[sceneName].NowIndex;
        }
        catch
        {
            return null;
        }
    }

    internal LongLiveSceneRouteResult ExecutePlayerWarp(LongLiveSceneAddress address, LongLiveSceneRouteResolution resolution)
    {
        try
        {
            switch (resolution.RouteKind)
            {
                case LongLiveSceneRouteKind.WorldMap:
                {
                    var appliedIndex = address.EntryIndex.GetValueOrDefault(101);
                    PlayerEx.Player.NowMapIndex = appliedIndex;
                    Tools.instance.loadMapScenes(address.SceneName, address.PreserveLastScene);
                    return LongLiveSceneRouteResult.Success(address.SceneName, resolution.RouteKind, address.EntryIndex, appliedIndex, "Loaded world map scene.");
                }

                case LongLiveSceneRouteKind.RegionScene:
                {
                    int? appliedIndex = null;
                    if (address.AutoResolveEntryIndex)
                    {
                        appliedIndex = TryResolveAutoWorldNodeIndex(address.SceneName, resolution.Descriptor);
                    }
                    else if (address.EntryIndex.HasValue && address.EntryIndex.Value > 0)
                    {
                        appliedIndex = address.EntryIndex.Value;
                    }

                    if (appliedIndex.HasValue)
                    {
                        PlayerEx.Player.NowMapIndex = appliedIndex.Value;
                    }

                    Tools.instance.loadMapScenes(address.SceneName, address.PreserveLastScene);
                    return LongLiveSceneRouteResult.Success(address.SceneName, resolution.RouteKind, address.EntryIndex, appliedIndex, "Loaded region scene.");
                }

                case LongLiveSceneRouteKind.SeaScene:
                {
                    PlayerEx.Player.NowMapIndex = 29;
                    var appliedIndex = address.EntryIndex ?? TryResolveSeaEntryIndex(address.SceneName, resolution.Descriptor);
                    if (!appliedIndex.HasValue || appliedIndex.Value <= 0)
                    {
                        return LongLiveSceneRouteResult.Failure(address.SceneName, resolution.RouteKind, address.EntryIndex, "missing-sea-entry-index", "Sea scene routing requires a valid entry index.");
                    }

                    SceneEx.LoadFuBen(address.SceneName, appliedIndex.Value);
                    return LongLiveSceneRouteResult.Success(address.SceneName, resolution.RouteKind, address.EntryIndex, appliedIndex, "Loaded sea scene.");
                }

                case LongLiveSceneRouteKind.DungeonScene:
                case LongLiveSceneRouteKind.RandomDungeonScene:
                {
                    var appliedIndex = address.EntryIndex.GetValueOrDefault(1);
                    if (appliedIndex <= 0)
                    {
                        appliedIndex = 1;
                    }

                    SceneEx.LoadFuBen(address.SceneName, appliedIndex);
                    return LongLiveSceneRouteResult.Success(address.SceneName, resolution.RouteKind, address.EntryIndex, appliedIndex, "Loaded dungeon scene.");
                }

                default:
                    return LongLiveSceneRouteResult.Failure(address.SceneName, resolution.RouteKind, address.EntryIndex, "unsupported-scene-kind", "The requested scene kind is not supported by the current host implementation.");
            }
        }
        catch (Exception exception)
        {
            return LongLiveSceneRouteResult.Failure(address.SceneName, resolution.RouteKind, address.EntryIndex, "exception", exception.GetType().Name + ": " + exception.Message);
        }
    }

    internal LongLiveSceneRouteResult ExecuteNpcWarp(int normalizedNpcId, LongLiveSceneAddress address, LongLiveSceneRouteResolution resolution)
    {
        try
        {
            switch (resolution.RouteKind)
            {
                case LongLiveSceneRouteKind.WorldMap:
                {
                    var appliedIndex = address.EntryIndex.GetValueOrDefault(PlayerEx.Player?.NowMapIndex ?? 0);
                    NPCEx.WarpToMap(normalizedNpcId, appliedIndex);
                    return LongLiveSceneRouteResult.Success(address.SceneName, resolution.RouteKind, address.EntryIndex, appliedIndex, "Warped NPC to world map node.");
                }

                case LongLiveSceneRouteKind.RegionScene:
                {
                    NPCEx.WarpToScene(normalizedNpcId, address.SceneName);
                    return LongLiveSceneRouteResult.Success(address.SceneName, resolution.RouteKind, address.EntryIndex, null, "Warped NPC to region scene.");
                }

                case LongLiveSceneRouteKind.DungeonScene:
                case LongLiveSceneRouteKind.RandomDungeonScene:
                case LongLiveSceneRouteKind.SeaScene:
                {
                    var appliedIndex = address.EntryIndex.GetValueOrDefault(0);
                    if (string.Equals(SafeGetCurrentSceneName(), address.SceneName, StringComparison.OrdinalIgnoreCase))
                    {
                        NPCEx.WarpToPlayerNowFuBen(normalizedNpcId, appliedIndex);
                        return LongLiveSceneRouteResult.Success(address.SceneName, resolution.RouteKind, address.EntryIndex, appliedIndex > 0 ? appliedIndex : TryResolveCurrentFuBenIndex(address.SceneName), "Warped NPC into the current fuben runtime.");
                    }

                    return LongLiveSceneRouteResult.Failure(address.SceneName, resolution.RouteKind, address.EntryIndex, "cross-fuben-npc-routing-unsupported", "NPC routing into a different fuben scene is not yet supported by the host implementation.");
                }

                default:
                    return LongLiveSceneRouteResult.Failure(address.SceneName, resolution.RouteKind, address.EntryIndex, "unsupported-scene-kind", "The requested NPC route kind is not supported by the current host implementation.");
            }
        }
        catch (Exception exception)
        {
            return LongLiveSceneRouteResult.Failure(address.SceneName, resolution.RouteKind, address.EntryIndex, "exception", exception.GetType().Name + ": " + exception.Message);
        }
    }

    private int? TryResolveAutoWorldNodeIndex(string sceneName, LongLiveSceneRouteDescriptor? descriptor)
    {
        try
        {
            if (descriptor is not null && descriptor.HostOutsideScenePos.HasValue)
            {
                return descriptor.HostOutsideScenePos.Value;
            }

            if (string.Equals(sceneName, "S101", StringComparison.OrdinalIgnoreCase))
            {
                if (DongFuManager.NowDongFuID == 1)
                {
                    return 98;
                }

                if (DongFuManager.NowDongFuID == 2)
                {
                    switch (PlayerEx.Player?.menPai)
                    {
                        case 1:
                            return 12;
                        case 3:
                            return 14;
                        case 4:
                            return 15;
                        case 5:
                            return 16;
                        case 6:
                            return 17;
                    }
                }
            }

            if (SceneNameJsonData.DataDict.TryGetValue(sceneName, out var metadata) && metadata is not null && metadata.OutsideScenePos > 0)
            {
                return metadata.OutsideScenePos;
            }
        }
        catch
        {
        }

        return null;
    }

    private int? TryResolveSeaEntryIndex(string sceneName, LongLiveSceneRouteDescriptor? descriptor)
    {
        try
        {
            if (descriptor is not null && descriptor.HostOutsideScenePos.HasValue)
            {
                return descriptor.HostOutsideScenePos.Value;
            }

            if (SceneNameJsonData.DataDict.TryGetValue(sceneName, out var metadata) && metadata is not null && metadata.OutsideScenePos > 0)
            {
                return metadata.OutsideScenePos;
            }
        }
        catch
        {
        }

        return null;
    }
}
