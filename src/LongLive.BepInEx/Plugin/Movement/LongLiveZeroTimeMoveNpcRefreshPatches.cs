using System;
using HarmonyLib;
using KBEngine;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

[HarmonyPatch(typeof(BaseMapCompont), nameof(BaseMapCompont.setAvatarNowMapIndex))]
internal static class LongLiveZeroTimeMoveNpcRefreshPatch
{
    private readonly struct MoveState
    {
        public MoveState(int oldMapIndex, string oldWorldTime, bool shouldTrack)
        {
            OldMapIndex = oldMapIndex;
            OldWorldTime = oldWorldTime;
            ShouldTrack = shouldTrack;
        }

        public int OldMapIndex { get; }

        public string OldWorldTime { get; }

        public bool ShouldTrack { get; }
    }

    private static bool Prepare()
    {
        return LongLivePlugin.Instance?.Options.EnableZeroTimeMoveNpcRefresh.Value == true;
    }

    private static void Prefix(ref MoveState __state)
    {
        var player = PlayerEx.Player;
        if (player == null)
        {
            __state = default;
            return;
        }

        __state = new MoveState(player.NowMapIndex, player.worldTimeMag?.nowTime ?? string.Empty, shouldTrack: true);
    }

    private static void Postfix(BaseMapCompont __instance, MoveState __state)
    {
        if (!__state.ShouldTrack)
        {
            return;
        }

        var player = PlayerEx.Player;
        var jieSuanManager = NpcJieSuanManager.inst;
        if (player == null || jieSuanManager == null)
        {
            return;
        }

        var newMapIndex = player.NowMapIndex;
        if (__state.OldMapIndex == newMapIndex)
        {
            return;
        }

        var sceneName = SceneManager.GetActiveScene().name;
        if (!string.Equals(sceneName, "AllMaps", StringComparison.Ordinal))
        {
            return;
        }

        var newWorldTime = player.worldTimeMag?.nowTime ?? string.Empty;
        if (!string.Equals(__state.OldWorldTime, newWorldTime, StringComparison.Ordinal))
        {
            return;
        }

        jieSuanManager.isUpDateNpcList = true;

        if (LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true)
        {
            LongLivePlugin.LogSource?.LogInfo(
                $"[ZeroTimeMoveNpcRefresh] Marked NPC list dirty after zero-time move. scene={sceneName}, from={__state.OldMapIndex}, to={newMapIndex}, node={__instance.NodeIndex}");
        }
    }
}
