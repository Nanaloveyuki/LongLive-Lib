using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCustomMapRuntimeActivationArtifactRegistry
{
    private static readonly Dictionary<string, LongLiveCustomMapRuntimeActivationArtifact> ArtifactsBySceneLogicalId = new Dictionary<string, LongLiveCustomMapRuntimeActivationArtifact>(StringComparer.Ordinal);

    private static ManualLogSource? _logger;

    public static void Initialize(ManualLogSource logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public static void ApplyReport(LongLiveCustomMapRuntimeActivationExecutionReport report, LongLiveCustomMapRuntimeActivationRuntimeSnapshot runtime)
    {
        if (report is null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        foreach (var result in report.Results)
        {
            if (!result.Succeeded)
            {
                continue;
            }

            var target = FindTarget(runtime, result.SceneLogicalId);
            if (target is null)
            {
                continue;
            }

            if (!ArtifactsBySceneLogicalId.TryGetValue(result.SceneLogicalId, out var artifact))
            {
                artifact = new LongLiveCustomMapRuntimeActivationArtifact
                {
                    SceneLogicalId = result.SceneLogicalId,
                    SceneName = target.SceneName,
                    OwningModId = target.OwningModId,
                    BoundEntrySceneName = target.SceneName,
                    Detail = target.Detail,
                };
            }

            artifact.ActivationState = result.ActivationState;
            artifact.StatusCode = result.StatusCode;
            artifact.HasPreparedHostSurface = artifact.HasPreparedHostSurface || result.ProducedHostPreparation;
            artifact.HasProxyRouteBinding = artifact.HasProxyRouteBinding || result.ProducedProxyBinding;
            artifact.BoundEntrySceneName = string.IsNullOrWhiteSpace(target.SceneName) ? artifact.BoundEntrySceneName : target.SceneName;
            artifact.BoundReturnSceneName = artifact.HasProxyRouteBinding ? target.SceneName : artifact.BoundReturnSceneName;
            artifact.Detail = result.Detail;
            ArtifactsBySceneLogicalId[result.SceneLogicalId] = artifact;
        }

        _logger?.LogInfo($"[CustomMapRuntimeActivationArtifacts] applied: artifacts={ArtifactsBySceneLogicalId.Count}, prepared={ArtifactsBySceneLogicalId.Values.Count(static artifact => artifact.HasPreparedHostSurface)}, bound={ArtifactsBySceneLogicalId.Values.Count(static artifact => artifact.HasProxyRouteBinding)}");
    }

    public static bool TryGet(string sceneLogicalId, out LongLiveCustomMapRuntimeActivationArtifact? artifact)
    {
        if (string.IsNullOrWhiteSpace(sceneLogicalId))
        {
            artifact = null;
            return false;
        }

        return ArtifactsBySceneLogicalId.TryGetValue(sceneLogicalId, out artifact);
    }

    public static LongLiveCustomMapRuntimeActivationArtifactSnapshot CaptureSnapshot(int sampleLimit = 8)
    {
        var artifacts = ArtifactsBySceneLogicalId.Values
            .OrderBy(static artifact => artifact.SceneLogicalId, StringComparer.Ordinal)
            .Take(sampleLimit)
            .ToArray();
        return new LongLiveCustomMapRuntimeActivationArtifactSnapshot
        {
            ArtifactCount = ArtifactsBySceneLogicalId.Count,
            PreparedSurfaceCount = ArtifactsBySceneLogicalId.Values.Count(static artifact => artifact.HasPreparedHostSurface),
            BoundProxyRouteCount = ArtifactsBySceneLogicalId.Values.Count(static artifact => artifact.HasProxyRouteBinding),
            Artifacts = artifacts,
        };
    }

    private static LongLiveCustomMapRuntimeActivationRuntimeTarget? FindTarget(LongLiveCustomMapRuntimeActivationRuntimeSnapshot runtime, string sceneLogicalId)
    {
        foreach (var target in runtime.Targets)
        {
            if (string.Equals(target.SceneLogicalId, sceneLogicalId, StringComparison.Ordinal))
            {
                return target;
            }
        }

        return null;
    }
}
