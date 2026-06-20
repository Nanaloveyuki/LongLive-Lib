using System;
using LongLive.Next.Abstractions.Commands;
using LongLive.Next.Abstractions.Queries;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveVToolsRoutingAdapter
{
    public const string RedirectId = "adapter.vtools.next-routing";

    private static bool _registered;

    private readonly NextRuntimeFacade _runtime;
    private readonly LongLiveCompatibilityRuntime _compatibilityRuntime;

    public LongLiveVToolsRoutingAdapter(NextRuntimeFacade runtime, LongLiveCompatibilityRuntime compatibilityRuntime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _compatibilityRuntime = compatibilityRuntime ?? throw new ArgumentNullException(nameof(compatibilityRuntime));
    }

    public bool IsAvailable => _runtime.IsAvailable && _runtime.CommandRegistry.IsAvailable && _runtime.QueryRegistry.IsAvailable;

    public static bool IsRegistered => _registered;

    public void Register()
    {
        if (!IsAvailable)
        {
            return;
        }

        if (_registered)
        {
            return;
        }

        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "PlayerWarp", HandlePlayerWarp, LongLiveCompatibilityOptionGate.IsVToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "NpcWarp", HandleNpcWarp, LongLiveCompatibilityOptionGate.IsVToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "PlayerWalk", HandlePlayerWalk, LongLiveCompatibilityOptionGate.IsVToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "PlayerMove", HandlePlayerMove, LongLiveCompatibilityOptionGate.IsVToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "SearchOneNpc", HandleSearchOneNpc, LongLiveCompatibilityOptionGate.IsVToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "AddShengWang", HandleAddShengWang, LongLiveCompatibilityOptionGate.IsVToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "CreateOneNpc", HandleCreateOneNpc, LongLiveCompatibilityOptionGate.IsVToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "GetCurAllMapIndex", HandleGetCurAllMapIndex, LongLiveCompatibilityOptionGate.IsVToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "GetCurFubenIndex", HandleGetCurFubenIndex, LongLiveCompatibilityOptionGate.IsVToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "GetPlaceName", HandleGetPlaceName, LongLiveCompatibilityOptionGate.IsVToolsEnabled, static () => string.Empty);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "GetNPCName", HandleGetNpcName, LongLiveCompatibilityOptionGate.IsVToolsEnabled, static () => string.Empty);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "RandomProbability", HandleRandomProbability, LongLiveCompatibilityOptionGate.IsVToolsEnabled, static () => false);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "NearNpcContains", HandleNearNpcContains, LongLiveCompatibilityOptionGate.IsVToolsEnabled, static () => false);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "PlayerHasDongFu", HandlePlayerHasDongFu, LongLiveCompatibilityOptionGate.IsVToolsEnabled, static () => false);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "GetShengWang", HandleGetShengWang, LongLiveCompatibilityOptionGate.IsVToolsEnabled, static () => 0);
        _registered = true;
    }

    private static void HandlePlayerWarp(NextCommandContext context, Action complete)
    {
        var sceneName = context.GetString(0, string.Empty);
        var entryIndex = context.TryGetString(1) is not null ? context.GetInt(1, 0) : 0;
        var hasEntryIndex = entryIndex > 0;

        var result = LongLiveNextRoutingBridge.WarpPlayer(string.Empty, sceneName, hasEntryIndex ? entryIndex : null, !hasEntryIndex);
        LongLiveNextEnvironmentBridge.WriteWarpResult(context.NativeEnvironment, "PlayerWarp", result);
        complete();
    }

    private static void HandleNpcWarp(NextCommandContext context, Action complete)
    {
        var npcId = context.GetInt(0, 0);
        var sceneName = context.GetString(1, string.Empty);
        var entryIndex = context.TryGetString(2) is not null ? context.GetInt(2, 0) : 0;
        var hasEntryIndex = entryIndex > 0;

        var result = LongLiveNextRoutingBridge.WarpNpc(npcId, string.Empty, sceneName, hasEntryIndex ? entryIndex : null, !hasEntryIndex);
        LongLiveNextEnvironmentBridge.WriteWarpResult(context.NativeEnvironment, "NpcWarp", result);
        complete();
    }

    private static void HandlePlayerWalk(NextCommandContext context, Action complete)
    {
        var index = context.GetInt(0, 0);
        LongLiveNextRoutingBridge.TryWalkPlayerOnWorldMap(index);
        complete();
    }

    private static void HandlePlayerMove(NextCommandContext context, Action complete)
    {
        var index = context.GetInt(0, 0);
        LongLiveNextRoutingBridge.TryMovePlayerToSceneEntry(index);
        complete();
    }

    private static void HandleSearchOneNpc(NextCommandContext context, Action complete)
    {
        var type = context.GetInt(0, 0);
        var liuPai = context.GetInt(1, 0);
        var level = context.GetInt(2, 0);
        var sex = context.GetInt(3, 0);
        var zhengXie = context.GetInt(4, 0);
        var roleId = LongLiveNextRoutingBridge.ResolveSearchNpc(type, liuPai, level, sex, zhengXie);
        var roleName = LongLiveNextRoutingBridge.ResolveNpcName(roleId);
        var roleBindId = roleId > 0 ? NPCEx.NPCIDToOld(roleId) : 0;

        LongLiveNextEnvironmentBridge.WriteRoleContext(context.NativeEnvironment, roleId, roleName, roleBindId);
        complete();
    }

    private static void HandleAddShengWang(NextCommandContext context, Action complete)
    {
        var shengWangId = context.GetInt(0, 0);
        var amount = context.GetInt(1, 0);
        LongLiveNextRoutingBridge.AddPlayerShengWang(shengWangId, amount);
        complete();
    }

    private static void HandleCreateOneNpc(NextCommandContext context, Action complete)
    {
        var type = context.GetInt(0, 0);
        var liuPai = context.GetInt(1, 0);
        var level = context.GetInt(2, 0);
        var sex = context.GetInt(3, 0);
        var zhengXie = context.GetInt(4, 0);
        var roleId = LongLiveNextRoutingBridge.ResolveCreateNpc(type, liuPai, level, sex, zhengXie);
        var roleName = LongLiveNextRoutingBridge.ResolveNpcName(roleId);
        var roleBindId = roleId > 0 ? NPCEx.NPCIDToOld(roleId) : 0;

        LongLiveNextEnvironmentBridge.WriteRoleContext(context.NativeEnvironment, roleId, roleName, roleBindId);
        complete();
    }

    private static object HandleGetCurAllMapIndex(NextQueryContext context)
    {
        return LongLiveNextRoutingBridge.ResolveCurrentMapIndex();
    }

    private static object HandleGetCurFubenIndex(NextQueryContext context)
    {
        return LongLiveNextRoutingBridge.ResolveCurrentFuBenIndex();
    }

    private static object HandleGetPlaceName(NextQueryContext context)
    {
        return LongLiveNextRoutingBridge.ResolveCurrentPlaceName();
    }

    private static object HandleGetNpcName(NextQueryContext context)
    {
        var npcId = context.GetInt(0, -1);
        return LongLiveNextRoutingBridge.ResolveNpcName(npcId);
    }

    private static object HandleRandomProbability(NextQueryContext context)
    {
        var threshold = context.GetInt(0, 100);
        return LongLiveNextRoutingBridge.RollProbability(threshold);
    }

    private static object HandleNearNpcContains(NextQueryContext context)
    {
        var argument = context.GetArgument(0);
        var probability = context.Arguments.Count > 1 ? context.GetInt(1, 100) : 100;

        if (!LongLiveNextRoutingBridge.ResolveNearNpcContains(context.NativeEnvironment, argument, probability, out var roleId, out var roleName, out var roleBindId))
        {
            return false;
        }

        LongLiveNextEnvironmentBridge.WriteRoleContext(context.NativeEnvironment, roleId, roleName, roleBindId);
        return true;
    }

    private static object HandlePlayerHasDongFu(NextQueryContext context)
    {
        var dongFuId = context.GetInt(0, 0);
        return LongLiveNextRoutingBridge.ResolvePlayerHasDongFu(dongFuId);
    }

    private static object HandleGetShengWang(NextQueryContext context)
    {
        var shengWangId = context.GetInt(0, 0);
        return LongLiveNextRoutingBridge.ResolvePlayerShengWang(shengWangId);
    }
}
