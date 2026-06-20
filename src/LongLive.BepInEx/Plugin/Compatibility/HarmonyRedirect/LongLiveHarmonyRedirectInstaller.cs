using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveHarmonyRedirectInstaller
{
    public static LongLiveHarmonyRedirectResult InstallPrefixSkip(
        ManualLogSource logger,
        LongLiveCompatibilityRuntime compatibilityRuntime,
        string harmonyId,
        string redirectId,
        string targetTypeName,
        string targetMethodName,
        MethodInfo? prefixMethod,
        params Type[] argumentTypes)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (compatibilityRuntime is null)
        {
            throw new ArgumentNullException(nameof(compatibilityRuntime));
        }

        if (prefixMethod is null)
        {
            return new LongLiveHarmonyRedirectResult(false, "missing-prefix", "Prefix method could not be resolved.");
        }

        var targetType = AccessTools.TypeByName(targetTypeName);
        var targetMethod = targetType is null
            ? null
            : AccessTools.Method(targetType, targetMethodName, argumentTypes ?? Type.EmptyTypes);

        if (targetMethod is null)
        {
            return new LongLiveHarmonyRedirectResult(false, "missing-target", $"Target method {targetTypeName}.{targetMethodName} could not be resolved.");
        }

        try
        {
            var harmony = new Harmony(harmonyId);
            harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefixMethod));
            compatibilityRuntime.RecordInvocation(redirectId, "redirect-installed", $"target={targetTypeName}.{targetMethodName}");
            logger.LogInfo($"LongLive compatibility redirect installed. redirect={redirectId}, target={targetTypeName}.{targetMethodName}");
            return new LongLiveHarmonyRedirectResult(true, "redirect-installed", $"Patched {targetTypeName}.{targetMethodName}.");
        }
        catch (Exception ex)
        {
            compatibilityRuntime.RecordInvocation(redirectId, "redirect-error", $"target={targetTypeName}.{targetMethodName}, error={ex.GetType().Name}");
            logger.LogWarning($"LongLive compatibility redirect failed. redirect={redirectId}, target={targetTypeName}.{targetMethodName}, error={ex.GetType().Name}: {ex.Message}");
            return new LongLiveHarmonyRedirectResult(false, "redirect-error", $"Patch failed with {ex.GetType().Name}: {ex.Message}");
        }
    }
}
