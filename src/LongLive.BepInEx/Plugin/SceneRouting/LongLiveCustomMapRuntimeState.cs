using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCustomMapRuntimeState
{
    private static ManualLogSource? _logger;
    private static LongLiveHostOptions? _options;

    public static void Initialize(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public static LongLiveCustomMapRuntimeStateSnapshot CaptureSnapshot(int sampleLimit = 8)
    {
        var feature = LongLivePluginContext.CustomMapRuntime;
        var activationPlan = LongLiveCustomMapRuntimeActivationPlanBuilder.Build(feature);
        var readiness = LongLiveCustomMapRuntimeReadinessEvaluator.Evaluate(activationPlan);
        var routingSnapshot = LongLivePluginContext.SceneRouting.CaptureSnapshot();
        var snapshot = new LongLiveCustomMapRuntimeStateSnapshot
        {
            ActiveSceneName = routingSnapshot.ActiveSceneName,
            TotalSceneCount = feature.Catalog.SceneCount,
            TotalBootstrapCount = feature.Bootstraps.Bootstraps.Count,
            TotalTopologyCount = feature.SceneLocalTopologies.TopologyCount,
            TotalTopologyNodeCount = feature.SceneLocalTopologies.NodeCount,
            RegisteredModIds = BuildRegisteredModIds(feature),
            RuntimeBootstrapSamples = BuildBootstrapSamples(feature, sampleLimit),
            ExternalActivationTargetCount = activationPlan.ExternalActivationTargetCount,
            ActivationPlan = activationPlan,
            Readiness = readiness,
        };

        if (!string.IsNullOrWhiteSpace(routingSnapshot.RegisteredSceneLogicalId) && feature.Catalog.TryGetByLogicalId(routingSnapshot.RegisteredSceneLogicalId, out var registeredScene) && registeredScene is not null)
        {
            snapshot.HasRegisteredScene = true;
            snapshot.ActiveSceneLogicalId = registeredScene.LogicalId;
            snapshot.ActiveOwningModId = registeredScene.OwningModId;
        }
        else if (!string.IsNullOrWhiteSpace(routingSnapshot.ActiveSceneName) && feature.Catalog.TryGetBySceneName(routingSnapshot.ActiveSceneName, out registeredScene) && registeredScene is not null)
        {
            snapshot.HasRegisteredScene = true;
            snapshot.ActiveSceneLogicalId = registeredScene.LogicalId;
            snapshot.ActiveOwningModId = registeredScene.OwningModId;
        }

        if (!string.IsNullOrWhiteSpace(snapshot.ActiveSceneLogicalId) && feature.Bootstraps.TryGetBySceneLogicalId(snapshot.ActiveSceneLogicalId, out var bootstrap) && bootstrap is not null)
        {
            ApplyBootstrap(snapshot, bootstrap);
        }
        else if (!string.IsNullOrWhiteSpace(snapshot.ActiveSceneName) && feature.Bootstraps.TryGetBySceneName(snapshot.ActiveSceneName, out bootstrap) && bootstrap is not null)
        {
            ApplyBootstrap(snapshot, bootstrap);
        }

        return snapshot;
    }

    public static void LogInstallerSummary()
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[CustomMapRuntime] ready: scenes={snapshot.TotalSceneCount}, bootstraps={snapshot.TotalBootstrapCount}, topologies={snapshot.TotalTopologyCount}, topologyNodes={snapshot.TotalTopologyNodeCount}, externalTargets={snapshot.ExternalActivationTargetCount}, readinessReady={snapshot.Readiness.ReadyTargetCount}, readinessBlocked={snapshot.Readiness.BlockedTargetCount}, activeScene={snapshot.ActiveSceneName}, activeBootstrap={(snapshot.HasRuntimeBootstrap ? snapshot.RuntimeBootstrapSceneLogicalId : "n/a")}");
        LogVerboseSnapshot(snapshot, "installer");
    }

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[CustomMapRuntime] sceneLoaded: scene={scene.name}, mode={mode}, registeredScene={snapshot.HasRegisteredScene}, activeSceneLogicalId={snapshot.ActiveSceneLogicalId}, activeBootstrap={(snapshot.HasRuntimeBootstrap ? snapshot.RuntimeBootstrapSceneLogicalId : "n/a")}, returnScene={(string.IsNullOrWhiteSpace(snapshot.RuntimeBootstrapReturnSceneName) ? "n/a" : snapshot.RuntimeBootstrapReturnSceneName)}, externalTargets={snapshot.ExternalActivationTargetCount}, activeReadiness={(string.IsNullOrWhiteSpace(snapshot.Readiness.ActiveStatusCode) ? "n/a" : snapshot.Readiness.ActiveStatusCode)}");
        LogVerboseSnapshot(snapshot, "sceneLoaded");
    }

    private static void ApplyBootstrap(LongLiveCustomMapRuntimeStateSnapshot snapshot, LongLive.Mods.Maps.LongLiveCustomMapRuntimeBootstrapDescriptor bootstrap)
    {
        snapshot.HasRuntimeBootstrap = bootstrap.CanBootstrapRuntime;
        snapshot.RuntimeBootstrapSceneLogicalId = bootstrap.SceneLogicalId;
        snapshot.RuntimeBootstrapEntryNodeLogicalId = bootstrap.EntryNodeLogicalId;
        snapshot.RuntimeBootstrapReturnSceneLogicalId = bootstrap.ReturnSceneLogicalId;
        snapshot.RuntimeBootstrapReturnSceneName = bootstrap.ReturnSceneName;

        if (string.IsNullOrWhiteSpace(snapshot.ActiveOwningModId))
        {
            snapshot.ActiveOwningModId = bootstrap.OwningModId;
        }
    }

    private static void LogVerboseSnapshot(LongLiveCustomMapRuntimeStateSnapshot snapshot, string source)
    {
        if (!IsVerbose())
        {
            return;
        }

        var mods = snapshot.RegisteredModIds.Count == 0 ? "n/a" : string.Join(", ", snapshot.RegisteredModIds.ToArray());
        var bootstraps = snapshot.RuntimeBootstrapSamples.Count == 0 ? "n/a" : string.Join(" | ", snapshot.RuntimeBootstrapSamples.ToArray());
        var activationTargets = snapshot.ActivationPlan.Targets.Count == 0
            ? "n/a"
            : string.Join(" | ", snapshot.ActivationPlan.Targets.Take(6).Select(static target => target.SceneLogicalId + ":activate=" + target.RequiresCustomActivation + ",return=" + (string.IsNullOrWhiteSpace(target.ReturnSceneName) ? "n/a" : target.ReturnSceneName)).ToArray());
        var readinessTargets = snapshot.Readiness.Targets.Count == 0
            ? "n/a"
            : string.Join(" | ", snapshot.Readiness.Targets.Take(6).Select(static target => target.SceneLogicalId + ":" + target.StatusCode + ",enter=" + target.CanEnterNow + ",host=" + target.IsHostBackedScene).ToArray());
        _logger?.LogInfo($"[CustomMapRuntime] {source} detail: activeScene={snapshot.ActiveSceneName}, activeOwningMod={snapshot.ActiveOwningModId}, bootstrapEntry={snapshot.RuntimeBootstrapEntryNodeLogicalId}, bootstrapReturn={snapshot.RuntimeBootstrapReturnSceneName}, registeredMods={mods}, bootstrapSamples={bootstraps}, activationTargets={activationTargets}, readinessTargets={readinessTargets}");
    }

    private static List<string> BuildRegisteredModIds(LongLive.Mods.Maps.ILongLiveCustomMapRuntimeFeature feature)
    {
        return feature.Catalog.Scenes
            .Select(static scene => scene.OwningModId)
            .Concat(feature.Bootstraps.Bootstraps.Select(static bootstrap => bootstrap.OwningModId))
            .Concat(feature.SceneLocalTopologies.Topologies.Select(static topology => topology.OwningModId))
            .Where(static modId => !string.IsNullOrWhiteSpace(modId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static modId => modId, StringComparer.Ordinal)
            .ToList();
    }

    private static List<string> BuildBootstrapSamples(LongLive.Mods.Maps.ILongLiveCustomMapRuntimeFeature feature, int sampleLimit)
    {
        return feature.Bootstraps.Bootstraps
            .Where(static bootstrap => bootstrap.CanBootstrapRuntime)
            .Take(sampleLimit)
            .Select(static bootstrap => bootstrap.SceneLogicalId + "->" + (string.IsNullOrWhiteSpace(bootstrap.ReturnSceneName) ? "n/a" : bootstrap.ReturnSceneName))
            .ToList();
    }

    private static bool IsEnabled()
    {
        return _logger is not null
            && _options?.EnableDebugLogging.Value == true
            && _options.EnableCustomMapRuntimeLogging.Value;
    }

    private static bool IsVerbose()
    {
        return IsEnabled() && _options?.EnableCustomMapRuntimeVerbose.Value == true;
    }
}
