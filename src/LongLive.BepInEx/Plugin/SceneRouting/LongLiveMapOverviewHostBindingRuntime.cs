using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapOverviewHostBindingRuntime
{
    private static ManualLogSource? _logger;
    private static LongLiveHostOptions? _options;

    public static void Initialize(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public static LongLiveMapOverviewHostBindingRuntimeSnapshot CaptureSnapshot(int sampleLimit = 8)
    {
        var installPlan = LongLivePluginContext.GetMapOverviewInstallPlan();
        var probeCaptured = LongLiveMapOverviewHostProbe.TryCapture(out var probe);

        var snapshot = new LongLiveMapOverviewHostBindingRuntimeSnapshot
        {
            ActiveSceneName = installPlan.ActiveSceneName,
            HasUiMapPanel = probe.HasUiMapPanel,
            HasNingZhouHost = probe.HasNingZhou,
            HasSeaHost = probe.HasSea,
            HasNingZhouAnchor = probe.HasNingZhouInjectionAnchor,
            HasSeaAnchor = probe.HasSeaInjectionAnchor,
            PanelObjectName = probe.PanelObjectName,
            ProbeError = probeCaptured ? string.Empty : probe.ProbeError,
            TotalBindingTargetCount = installPlan.PageTargets.Count,
            ExternalBindingTargetCount = installPlan.ExternalPageTargetCount,
        };

        var targets = new List<LongLiveMapOverviewHostBindingTarget>();
        foreach (var pageTarget in installPlan.PageTargets.Take(sampleLimit))
        {
            var expectsSea = pageTarget.PageId.IndexOf("sea", StringComparison.OrdinalIgnoreCase) >= 0;
            var hasExpectedHost = expectsSea ? probe.HasSea : probe.HasNingZhou;
            var hasAnchor = expectsSea ? probe.HasSeaInjectionAnchor : probe.HasNingZhouInjectionAnchor;
            targets.Add(new LongLiveMapOverviewHostBindingTarget
            {
                PageId = pageTarget.PageId,
                OwningModId = pageTarget.OwningModId,
                DisplayName = pageTarget.DisplayName,
                RequiresHostInjection = pageTarget.RequiresHostInjection,
                ExpectsSeaHost = expectsSea,
                HasExpectedHostRoot = hasExpectedHost,
                HasInjectionAnchor = hasAnchor,
                InjectionAnchorName = expectsSea ? probe.SeaInjectionAnchorName : probe.NingZhouInjectionAnchorName,
                HostRootName = expectsSea ? probe.SeaNodeRootName : probe.NingZhouNodeRootName,
                HighlightRootName = expectsSea ? probe.SeaHighlightRootName : probe.NingZhouHighlightRootName,
                NodeChildCount = expectsSea ? probe.SeaNodeChildCount : probe.NingZhouNodeChildCount,
                HighlightChildCount = expectsSea ? probe.SeaHighlightChildCount : probe.NingZhouHighlightChildCount,
                ProjectionCount = pageTarget.ProjectionCount,
                NodeCount = pageTarget.NodeCount,
                RegionCount = pageTarget.RegionCount,
                IsActivePageTarget = pageTarget.ContainsActiveProjection,
                HierarchySample = expectsSea ? probe.SeaHierarchySample : probe.NingZhouHierarchySample,
            });
        }

        snapshot.Targets = targets;
        snapshot.BindableTargetCount = targets.Count(static target => !target.RequiresHostInjection || (target.HasExpectedHostRoot && target.HasInjectionAnchor));
        snapshot.MissingAnchorTargetCount = targets.Count(static target => target.RequiresHostInjection && target.HasExpectedHostRoot && !target.HasInjectionAnchor);
        return snapshot;
    }

    public static void LogInstallerSummary()
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[MapOverviewBinding] ready: targets={snapshot.TotalBindingTargetCount}, externalTargets={snapshot.ExternalBindingTargetCount}, bindableTargets={snapshot.BindableTargetCount}, missingAnchorTargets={snapshot.MissingAnchorTargetCount}, panel={(string.IsNullOrWhiteSpace(snapshot.PanelObjectName) ? "n/a" : snapshot.PanelObjectName)}");
        LogVerboseSnapshot(snapshot, "installer");
    }

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[MapOverviewBinding] sceneLoaded: scene={scene.name}, mode={mode}, panel={snapshot.HasUiMapPanel}, ningzhouAnchor={snapshot.HasNingZhouAnchor}, seaAnchor={snapshot.HasSeaAnchor}, bindableTargets={snapshot.BindableTargetCount}, missingAnchorTargets={snapshot.MissingAnchorTargetCount}");
        LogVerboseSnapshot(snapshot, "sceneLoaded");
    }

    private static void LogVerboseSnapshot(LongLiveMapOverviewHostBindingRuntimeSnapshot snapshot, string source)
    {
        if (!IsVerbose())
        {
            return;
        }

        var targets = snapshot.Targets.Count == 0
            ? "n/a"
            : string.Join(" | ", snapshot.Targets.Select(static target =>
                target.PageId + ":inject=" + target.RequiresHostInjection + ",host=" + (target.ExpectsSeaHost ? "Sea" : "NingZhou") + ",anchor=" + target.HasInjectionAnchor + ",root=" + (string.IsNullOrWhiteSpace(target.HostRootName) ? "n/a" : target.HostRootName)).ToArray());
        _logger?.LogInfo($"[MapOverviewBinding] {source} detail: activeScene={snapshot.ActiveSceneName}, probeError={(string.IsNullOrWhiteSpace(snapshot.ProbeError) ? "n/a" : snapshot.ProbeError)}, targets={targets}");
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
