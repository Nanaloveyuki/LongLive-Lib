using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using Fight;
using GUIPackage;
using YSGame.Fight;

namespace LongLive.BepInEx.Plugin;

[HarmonyPatch(typeof(Animator), nameof(Animator.Play), new[] { typeof(string) })]
internal static class LongLiveFadeAnimatorPlayPatch
{
    private static void Prefix(Animator __instance, string stateName)
    {
        LongLiveFadeOptimizationRuntime.ReconcileAnimatorSpeed(__instance, stateName);
    }
}

[HarmonyPatch(typeof(Animator), nameof(Animator.Play), new[] { typeof(string), typeof(int), typeof(float) })]
internal static class LongLiveFadeAnimatorPlayExtendedPatch
{
    private static void Prefix(Animator __instance, string stateName)
    {
        LongLiveFadeOptimizationRuntime.ReconcileAnimatorSpeed(__instance, stateName);
    }
}

[HarmonyPatch(typeof(Tools), nameof(Tools.loadMapScenes))]
internal static class LongLiveFadeLoadMapScenesPatch
{
    private static bool Prefix(Tools __instance, string name, bool LastSceneIsValue)
    {
        if (LongLiveFadeOptimizationRuntime.TryHandleAsyncMapSceneLoad(__instance, name, LastSceneIsValue))
        {
            return false;
        }

        return true;
    }

    private static void Postfix()
    {
        LongLiveFadeOptimizationRuntime.ReconcileBlackMaskReplay(PanelMamager.inst?.UIBlackMaskGameObject, "Tools.loadMapScenes");
    }
}

[HarmonyPatch(typeof(Loading), nameof(Loading.UcitanaScena))]
internal static class LongLiveFadeLoadingSceneEntryPatch
{
    private static bool Prefix(Loading __instance, Camera camera, int skaliraj, float delay, ref IEnumerator __result)
    {
        if (LongLiveFadeOptimizationRuntime.TryCreateLoadingSceneEntryRoutine(__instance, camera, skaliraj, delay, out var routine) && routine != null)
        {
            __result = routine;
            return false;
        }

        return true;
    }

    private static void PrefixScaleDelay(ref float delay)
    {
        if (!LongLiveFadeOptimizationRuntime.IsEnabled)
        {
            return;
        }

        delay = LongLiveFadeOptimizationRuntime.ScaleDuration(delay);
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            instructions,
            0.65f,
            "Loading.UcitanaScena.outroDelay_0_65",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.GetLoadingSceneTipToDoorDelay))!);
    }
}

[HarmonyPatch(typeof(Loading), "unistiObjekat")]
internal static class LongLiveFadeLoadingSceneExitPrefixPatch
{
    private static void PrefixScaleDelay(ref float time)
    {
        if (!LongLiveFadeOptimizationRuntime.IsEnabled)
        {
            return;
        }

        time = LongLiveFadeOptimizationRuntime.ScaleDuration(time);
    }

}

[HarmonyPatch(typeof(Loading), "unistiObjekat")]
internal static class LongLiveFadeLoadingSceneExitTimePrefixPatch
{
    private static void PrefixScaleByScene(ref float time, int kojaScena)
    {
        if (!LongLiveFadeOptimizationRuntime.IsEnabled)
        {
            return;
        }

        time = LongLiveFadeOptimizationRuntime.GetLoadingSceneOutroDelay(time, kojaScena);
    }
}

[HarmonyPatch]
internal static class LongLiveFadeLoadingSceneExitPatch
{
    private static MethodBase TargetMethod()
    {
        return LongLiveFadePatchTargetResolver.GetEnumeratorMoveNext(typeof(Loading), "unistiObjekat", typeof(float), typeof(int));
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var patched = LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            instructions,
            1f,
            "Loading.unistiObjekat.delay",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.GetMapDoorTransitionDelay))!);

        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            patched,
            1f,
            "Loading.unistiObjekat.destroyDelay",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.GetLoadingSceneTipToDoorDelay))!);
    }
}

[HarmonyPatch]
internal static class LongLiveFadeAllMapsDoorPatch
{
    private static MethodBase TargetMethod()
    {
        return LongLiveFadePatchTargetResolver.GetEnumeratorMoveNext(typeof(AllMapsManageFull), "UcitajOstrvo", typeof(string));
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            instructions,
            1.1f,
            "AllMapsManageFull.UcitajOstrvo.delay",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.GetMapDoorTransitionDelay))!);
    }
}

[HarmonyPatch]
internal static class LongLiveFadeKameraIslandDoorPatch
{
    private static MethodBase TargetMethod()
    {
        return LongLiveFadePatchTargetResolver.GetEnumeratorMoveNext(typeof(KameraMovement), "UcitajOstrvo", typeof(string));
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            instructions,
            1.1f,
            "KameraMovement.UcitajOstrvo.delay",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.GetMapDoorTransitionDelay))!);
    }
}

[HarmonyPatch]
internal static class LongLiveFadeKameraCloseDoorPatch
{
    private static MethodBase TargetMethod()
    {
        return LongLiveFadePatchTargetResolver.GetEnumeratorMoveNext(typeof(KameraMovement), "closeDoorAndPlay");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            instructions,
            0.75f,
            "KameraMovement.closeDoorAndPlay.delay",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.GetMapDoorTransitionDelay))!);
    }
}

[HarmonyPatch]
internal static class LongLiveFadeManageCloseDoorPatch
{
    private static MethodBase TargetMethod()
    {
        return LongLiveFadePatchTargetResolver.GetEnumeratorMoveNext(typeof(Manage), "closeDoorAndPlay");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            instructions,
            0.75f,
            "Manage.closeDoorAndPlay.delay",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.GetMapDoorTransitionDelay))!);
    }
}

[HarmonyPatch]
internal static class LongLiveFadeMainSceneDoorPatch
{
    private static MethodBase TargetMethod()
    {
        return LongLiveFadePatchTargetResolver.GetEnumeratorMoveNext(typeof(MainScene), "otvoriSledeciNivo");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            instructions,
            1.1f,
            "MainScene.otvoriSledeciNivo.delay",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.GetMapDoorTransitionDelay))!);
    }
}

[HarmonyPatch]
internal static class LongLiveFadeMainMenuManageDoorPatch
{
    private static MethodBase TargetMethod()
    {
        return LongLiveFadePatchTargetResolver.GetEnumeratorMoveNext(typeof(MainMenuManage), "otvoriSledeciNivo");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            instructions,
            0.25f,
            "MainMenuManage.otvoriSledeciNivo.delay",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.ScaleDuration))!);
    }
}

[HarmonyPatch(typeof(JueSuanAnimation), nameof(JueSuanAnimation.Play))]
internal static class LongLiveFadeJieSuanAnimationPlayPatch
{
    private static void Postfix(JueSuanAnimation __instance)
    {
        LongLiveFadeOptimizationRuntime.TryAccelerateTweenField(__instance, "obj", "JueSuanAnimation.Play.tween");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var scaleMethod = AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.ScaleDuration))!;
        var patched = LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(instructions, 1.5f, "JueSuanAnimation.Play.fill_0_2", scaleMethod);
        patched = LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(patched, 0.5f, "JueSuanAnimation.Play.fill_1_0", scaleMethod);
        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(patched, 20f, "JueSuanAnimation.Play.fill_0_98", scaleMethod);
    }
}

[HarmonyPatch(typeof(JueSuanAnimation), "CallBack")]
internal static class LongLiveFadeJieSuanAnimationCallbackPatch
{
    private static void Postfix(JueSuanAnimation __instance)
    {
        LongLiveFadeOptimizationRuntime.TryAccelerateTweenField(__instance, "obj", "JueSuanAnimation.CallBack.tween");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            instructions,
            0.5f,
            "JueSuanAnimation.CallBack.fill_1_0",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.ScaleDuration))!);
    }
}

[HarmonyPatch(typeof(UIAnimProgressBar), "Update")]
internal static class LongLiveFadeUiAnimProgressBarUpdatePatch
{
    private static void Postfix(UIAnimProgressBar __instance)
    {
        LongLiveFadeOptimizationRuntime.TryAccelerateTweenField(__instance, "obj", "UIAnimProgressBar.Update.tween");
    }
}

[HarmonyPatch(typeof(FightResultMag), nameof(FightResultMag.ShowVictory))]
internal static class LongLiveFadeFightResultShowVictoryPatch
{
    private static void Prefix(FightResultMag __instance)
    {
        if (!LongLiveFadeOptimizationRuntime.IsEnabled)
        {
            return;
        }

        __instance.CancelInvoke("LaterShowVictory");
    }

    private static void Postfix(FightResultMag __instance)
    {
        if (!LongLiveFadeOptimizationRuntime.IsEnabled)
        {
            return;
        }

        __instance.CancelInvoke("LaterShowVictory");
        __instance.Invoke("LaterShowVictory", LongLiveFadeOptimizationRuntime.ScaleDuration(1f));
    }
}

[HarmonyPatch(typeof(FightVictory), "Close")]
internal static class LongLiveFadeFightVictoryClosePatch
{
    private static bool Prefix(FightVictory __instance)
    {
        if (!LongLiveFadeOptimizationRuntime.TryHandleSameSceneMapReturn(Tools.instance?.FinalScene))
        {
            return true;
        }

        ESCCloseManager.Inst.UnRegisterClose(__instance);
        UnityEngine.Object.Destroy(__instance.gameObject);
        return false;
    }
}

[HarmonyPatch(typeof(UI_Manager), "Update")]
internal static class LongLiveFadeImmediateFightSceneEntryPatch
{
    private static bool Prefix(UI_Manager __instance)
    {
        return !LongLiveFadeOptimizationRuntime.TryHandleImmediateFightSceneEntry(__instance);
    }

    private static void Postfix()
    {
        LongLiveFadeOptimizationRuntime.ReconcileBlackMaskReplay(PanelMamager.inst?.UIBlackMaskGameObject, "UI_Manager.Update");
    }
}

[HarmonyPatch(typeof(UI_Manager), nameof(UI_Manager.hideBlack))]
internal static class LongLiveFadeUiManagerHideBlackPatch
{
    private static void Postfix(UI_Manager __instance)
    {
        LongLiveFadeOptimizationRuntime.ReconcileUiBlackOverlay(__instance.BlackGameObject, "UI_Manager.hideBlack");
    }
}

[HarmonyPatch]
internal static class LongLiveFadeRoundManagerDelayStartPatch
{
    private static MethodBase TargetMethod()
    {
        return LongLiveFadePatchTargetResolver.GetEnumeratorMoveNext(typeof(RoundManager), "DelayStart", typeof(KBEngine.Avatar));
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            instructions,
            0.1f,
            "RoundManager.DelayStart.delay",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.GetBattleReadyDelay))!);
    }
}

[HarmonyPatch(typeof(UIFightRoundCount), nameof(UIFightRoundCount.ShowRuond))]
internal static class LongLiveFadeUiFightRoundCountPatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return LongLiveFadeTranspilerTools.WrapFloatConstantWithCall(
            instructions,
            0.5f,
            "UIFightRoundCount.ShowRuond.tween_0_5",
            AccessTools.Method(typeof(LongLiveFadeOptimizationRuntime), nameof(LongLiveFadeOptimizationRuntime.ScaleDuration))!);
    }
}

internal static class LongLiveFadePatchTargetResolver
{
    public static MethodBase GetEnumeratorMoveNext(Type declaringType, string methodName, params Type[] parameterTypes)
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var method = declaringType.GetMethod(methodName, bindingFlags, null, parameterTypes ?? Type.EmptyTypes, null);
        if (method == null)
        {
            var signature = parameterTypes == null || parameterTypes.Length == 0
                ? "()"
                : $"({string.Join(", ", Array.ConvertAll(parameterTypes, static type => type.Name))})";

            throw new MissingMethodException(declaringType.FullName, methodName + signature);
        }

        var stateMachine = method.GetCustomAttribute<IteratorStateMachineAttribute>();
        if (stateMachine?.StateMachineType == null)
        {
            throw new InvalidOperationException($"Iterator state machine not found for {declaringType.FullName}.{methodName}().");
        }

        var moveNext = AccessTools.Method(stateMachine.StateMachineType, nameof(System.Collections.IEnumerator.MoveNext), Type.EmptyTypes);
        if (moveNext == null)
        {
            throw new MissingMethodException(stateMachine.StateMachineType.FullName, nameof(System.Collections.IEnumerator.MoveNext));
        }

        return moveNext;
    }
}

internal static class LongLiveFadeTranspilerTools
{
    public static IEnumerable<CodeInstruction> WrapFloatConstantWithCall(IEnumerable<CodeInstruction> instructions, float targetValue, string patchKey, System.Reflection.MethodInfo method)
    {
        var hitCount = 0;
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float value && Math.Abs(value - targetValue) < 0.0001f)
            {
                hitCount++;
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Call, method);
                continue;
            }

            yield return instruction;
        }

        LongLiveFadeOptimizationRuntime.ReportTranspilerPatchResult(patchKey, targetValue, hitCount);
    }
}
