using System;
using LongLive.Next.Abstractions.Commands;
using LongLive.Next.Abstractions.Queries;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveWhiteZeRoutingAdapter
{
    public const string RedirectId = "adapter.whiteze.next-routing";

    private static bool _registered;

    private readonly NextRuntimeFacade _runtime;
    private readonly LongLiveCompatibilityRuntime _compatibilityRuntime;

    public LongLiveWhiteZeRoutingAdapter(NextRuntimeFacade runtime, LongLiveCompatibilityRuntime compatibilityRuntime)
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

        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "IS_PlayerWarp", HandlePlayerWarp, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "IS_NpcWarp", HandleNpcWarp, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "IS_PlayerWalk", HandlePlayerWalk, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "IS_PlayerMove", HandlePlayerMove, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterCommandAlias(_compatibilityRuntime, _runtime.CommandRegistry, RedirectId, "IS_AddShengWang", HandleAddShengWang, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_GetCurAllMapIndex", HandleGetCurAllMapIndex, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_GetCurFubenIndex", HandleGetCurFubenIndex, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_GetCurSceneName", HandleGetCurSceneName, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => string.Empty);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_GetPlaceName", HandleGetPlaceName, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => string.Empty);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_GetMapType", HandleGetMapType, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_GetRand", HandleGetRand, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_GetNpcNewId", HandleGetNpcNewId, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_GetTMYYear", HandleGetTmyYear, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => false);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_HasItem", HandleHasItem, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => false);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_HasMod", HandleHasMod, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => false);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_SearchOneNpc", HandleSearchOneNpc, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_SearchOneNpcByFav", HandleSearchOneNpcByFav, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_CreateOneNpc", HandleCreateOneNpc, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_NpcSayName", HandleNpcSayName, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => "道友");
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_NpcSayNameFirstName", HandleNpcSayNameFirstName, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => "道友");
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "IS_PlayerSayNpcName", HandlePlayerSayNpcName, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => "道友");
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "GetCurAllMapIndex", HandleGetCurAllMapIndex, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "GetCurFubenIndex", HandleGetCurFubenIndex, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => 0);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "GetCurSceneName", HandleGetCurSceneName, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => string.Empty);
        LongLiveNextCompatibilityRegistration.RegisterQueryAlias(_compatibilityRuntime, _runtime.QueryRegistry, RedirectId, "GetPlaceName", HandleGetPlaceName, LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled, static () => string.Empty);
        _registered = true;
    }

    private static void HandlePlayerWarp(NextCommandContext context, Action complete)
    {
        var logicalSceneId = context.GetString(0, string.Empty);
        var sceneName = context.GetString(1, string.Empty);
        var entryIndex = context.TryGetString(2) is not null ? context.GetInt(2, 0) : 0;
        var hasEntryIndex = entryIndex > 0;

        var result = LongLiveNextRoutingBridge.WarpPlayer(logicalSceneId, sceneName, hasEntryIndex ? entryIndex : null, !hasEntryIndex);
        LongLiveNextEnvironmentBridge.WriteWarpResult(context.NativeEnvironment, "PlayerWarp", result);
        complete();
    }

    private static void HandleNpcWarp(NextCommandContext context, Action complete)
    {
        var npcId = context.GetInt(0, 0);
        var logicalSceneId = context.GetString(1, string.Empty);
        var sceneName = context.GetString(2, string.Empty);
        var entryIndex = context.TryGetString(3) is not null ? context.GetInt(3, 0) : 0;
        var hasEntryIndex = entryIndex > 0;

        var result = LongLiveNextRoutingBridge.WarpNpc(npcId, logicalSceneId, sceneName, hasEntryIndex ? entryIndex : null, !hasEntryIndex);
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

    private static void HandleAddShengWang(NextCommandContext context, Action complete)
    {
        var shengWangId = context.GetInt(0, 0);
        var amount = context.GetInt(1, 0);
        LongLiveNextRoutingBridge.AddPlayerShengWang(shengWangId, amount);
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

    private static object HandleGetCurSceneName(NextQueryContext context)
    {
        return LongLiveNextRoutingBridge.ResolveCurrentSceneName();
    }

    private static object HandleGetPlaceName(NextQueryContext context)
    {
        return LongLiveNextRoutingBridge.ResolveCurrentPlaceName();
    }

    private static object HandleGetMapType(NextQueryContext context)
    {
        return LongLiveNextRoutingBridge.ResolveCurrentMapType();
    }

    private static object HandleGetRand(NextQueryContext context)
    {
        var exclusiveMax = context.GetInt(0, 1);
        var inclusiveMin = context.Arguments.Count > 1 ? context.GetInt(1, 0) : 0;
        return LongLiveNextRoutingBridge.ResolveRandomInt(exclusiveMax, inclusiveMin);
    }

    private static object HandleGetNpcNewId(NextQueryContext context)
    {
        var npcId = context.GetInt(0, 0);
        return LongLiveNextRoutingBridge.ResolveNpcNewId(npcId);
    }

    private static object HandleGetTmyYear(NextQueryContext context)
    {
        return LongLiveNextRoutingBridge.ResolveIsTaiMiaoYear();
    }

    private static object HandleHasItem(NextQueryContext context)
    {
        var itemId = context.GetInt(0, 0);
        var isNpc = context.GetInt(1, 0) != 0;
        var npcId = context.GetInt(2, 0);
        return LongLiveNextRoutingBridge.ResolveHasItem(itemId, isNpc, npcId);
    }

    private static object HandleHasMod(NextQueryContext context)
    {
        var modName = context.GetString(0, string.Empty);
        return LongLiveNextRoutingBridge.ResolveHasWorkshopMod(modName);
    }

    private static object HandleSearchOneNpc(NextQueryContext context)
    {
        var type = context.GetInt(0, 0);
        var liuPai = context.GetInt(1, 0);
        var level = context.GetInt(2, 0);
        var sex = context.GetInt(3, 0);
        var zhengXie = context.GetInt(4, 0);
        return LongLiveNextRoutingBridge.ResolveSearchNpc(type, liuPai, level, sex, zhengXie);
    }

    private static object HandleSearchOneNpcByFav(NextQueryContext context)
    {
        var minimumFavor = context.GetInt(0, -201);
        return LongLiveNextRoutingBridge.ResolveSearchNpcByFavor(minimumFavor);
    }

    private static object HandleCreateOneNpc(NextQueryContext context)
    {
        var type = context.GetInt(0, 0);
        var liuPai = context.GetInt(1, 0);
        var level = context.GetInt(2, 0);
        var sex = context.GetInt(3, 0);
        var zhengXie = context.GetInt(4, 0);
        return LongLiveNextRoutingBridge.ResolveCreateNpc(type, liuPai, level, sex, zhengXie);
    }

    private static object HandleNpcSayName(NextQueryContext context)
    {
        var npcId = context.GetInt(0, 0);
        return LongLiveNextRoutingBridge.ResolveNpcSayName(npcId);
    }

    private static object HandleNpcSayNameFirstName(NextQueryContext context)
    {
        var npcId = context.GetInt(0, 0);
        return LongLiveNextRoutingBridge.ResolveNpcSayNameFirstName(npcId);
    }

    private static object HandlePlayerSayNpcName(NextQueryContext context)
    {
        var npcId = context.GetInt(0, 0);
        return LongLiveNextRoutingBridge.ResolvePlayerSayNpcName(npcId);
    }
}
