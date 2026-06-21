using System;
using System.Linq;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveSceneRoutingStartupReporter
{
    public static void LogSummary(ManualLogSource logger)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var registration = LongLivePluginContext.GetSceneRoutingRegistrationSnapshot();
        var overviewPlan = LongLivePluginContext.GetMapOverviewInstallPlan();
        var runtimePlan = LongLivePluginContext.GetCustomMapRuntimeActivationPlan();
        var runtimeActivation = LongLivePluginContext.GetCustomMapRuntimeActivationRuntimeSnapshot(int.MaxValue);

        logger.LogInfo(
            "LongLive scene-routing startup summary: " +
            $"routes={registration.RouteCount}, projections={registration.RouteProjectionCount}, runtimeScenes={registration.CustomRuntimeSceneCount}, bootstraps={registration.CustomRuntimeBootstrapCount}, " +
            $"overviewExternalPages={overviewPlan.ExternalPageTargetCount}, overviewExternalProjections={overviewPlan.ExternalProjectionCount}, " +
            $"runtimeExternalTargets={runtimePlan.ExternalActivationTargetCount}, runtimePending={runtimeActivation.PendingImplementationTargetCount}, runtimeBlocked={runtimeActivation.BlockedTargetCount}, activeScene={registration.ActiveSceneName}, activeLogicalId={(string.IsNullOrWhiteSpace(registration.ActiveSceneLogicalId) ? "n/a" : registration.ActiveSceneLogicalId)}");

        if (LongLivePlugin.Instance?.Options.EnableDebugLogging.Value != true)
        {
            return;
        }

        var activeOverviewTargets = overviewPlan.PageTargets
            .Where(static target => target.ContainsActiveProjection)
            .Select(static target => target.PageId)
            .ToArray();
        var externalOverviewTargets = overviewPlan.PageTargets
            .Where(static target => target.RequiresHostInjection)
            .Take(8)
            .Select(static target => target.PageId + "(" + target.OwningModId + ")")
            .ToArray();
        var runtimeTargets = runtimePlan.Targets
            .Where(static target => target.RequiresCustomActivation)
            .Take(8)
            .Select(static target => target.SceneLogicalId + "->" + (string.IsNullOrWhiteSpace(target.ReturnSceneName) ? "n/a" : target.ReturnSceneName))
            .ToArray();
        var activationTargets = runtimeActivation.Targets
            .Take(8)
            .Select(static target => target.SceneLogicalId + ":state=" + target.ActivationState + ",status=" + target.StatusCode)
            .ToArray();

        logger.LogInfo(
            "LongLive scene-routing startup detail: " +
            $"activeOverviewTargets={(activeOverviewTargets.Length == 0 ? "n/a" : string.Join(",", activeOverviewTargets))}, " +
            $"externalOverviewTargets={(externalOverviewTargets.Length == 0 ? "n/a" : string.Join(",", externalOverviewTargets))}, " +
            $"runtimeTargets={(runtimeTargets.Length == 0 ? "n/a" : string.Join(",", runtimeTargets))}, " +
            $"activationTargets={(activationTargets.Length == 0 ? "n/a" : string.Join(",", activationTargets))}");
    }
}
