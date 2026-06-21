using System;
using System.Linq;
using BepInEx.Logging;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapOverviewShellAllocationRuntime
{
    private static ManualLogSource? _logger;
    private static LongLiveHostOptions? _options;

    public static void Initialize(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public static LongLiveMapOverviewShellAllocationRuntimeSnapshot CaptureSnapshot(int sampleLimit = 8)
    {
        var installPlan = LongLivePluginContext.GetMapOverviewInstallPlan();
        var bindingRuntime = LongLivePluginContext.GetMapOverviewHostBindingRuntimeSnapshot(int.MaxValue);
        var allocationPlan = LongLiveMapOverviewShellAllocationPlanBuilder.Build(installPlan, bindingRuntime);

        return new LongLiveMapOverviewShellAllocationRuntimeSnapshot
        {
            ActiveSceneName = installPlan.ActiveSceneName,
            TargetCount = allocationPlan.TargetCount,
            ReuseExistingShellCount = allocationPlan.ReuseExistingShellCount,
            DedicatedShellCount = allocationPlan.DedicatedShellCount,
            BindableTargetCount = allocationPlan.BindableTargetCount,
            Targets = allocationPlan.Targets.Take(sampleLimit).ToArray(),
        };
    }

    public static void LogInstallerSummary()
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[MapOverviewShellAllocation] ready: targets={snapshot.TargetCount}, reuseShells={snapshot.ReuseExistingShellCount}, dedicatedShells={snapshot.DedicatedShellCount}, bindableTargets={snapshot.BindableTargetCount}");
        LogVerboseSnapshot(snapshot, "installer");
    }

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[MapOverviewShellAllocation] sceneLoaded: scene={scene.name}, mode={mode}, reuseShells={snapshot.ReuseExistingShellCount}, dedicatedShells={snapshot.DedicatedShellCount}, bindableTargets={snapshot.BindableTargetCount}");
        LogVerboseSnapshot(snapshot, "sceneLoaded");
    }

    private static void LogVerboseSnapshot(LongLiveMapOverviewShellAllocationRuntimeSnapshot snapshot, string source)
    {
        if (!IsVerbose())
        {
            return;
        }

        var targets = snapshot.Targets.Count == 0
            ? "n/a"
            : string.Join(" | ", snapshot.Targets.Select(static target => target.PageId + ":shell=" + target.ShellKind + ",host=" + target.HostSurface + ",bindable=" + target.CanBindInCurrentSession).ToArray());
        _logger?.LogInfo($"[MapOverviewShellAllocation] {source} detail: activeScene={snapshot.ActiveSceneName}, targets={targets}");
    }

    private static bool IsEnabled()
    {
        return _logger is not null
            && _options?.EnableDebugLogging.Value == true
            && _options.EnableMapOverviewRuntimeLogging.Value;
    }

    private static bool IsVerbose()
    {
        return IsEnabled() && _options?.EnableMapOverviewRuntimeVerbose.Value == true;
    }
}
