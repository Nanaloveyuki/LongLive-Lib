using System;
using System.Collections.Generic;
using BepInEx.Logging;
using LongLive.Mods.Maps;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveSceneLocalTopologyRuntime
{
    private static ManualLogSource? _logger;
    private static LongLiveHostOptions? _options;

    public static void Initialize(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public static LongLiveSceneLocalTopologyRuntimeSnapshot CaptureSnapshot(int sampleLimit = 8)
    {
        var topologyCatalog = LongLivePluginContext.CustomMapRuntime.SceneLocalTopologies;
        var runtimeCatalog = LongLivePluginContext.CustomMapRuntime.Catalog;
        var activeSceneName = SafeGetCurrentSceneName();

        var snapshot = new LongLiveSceneLocalTopologyRuntimeSnapshot
        {
            ActiveSceneName = activeSceneName,
            TotalTopologyCount = topologyCatalog.TopologyCount,
            TotalNodeCount = topologyCatalog.NodeCount,
        };

        LongLiveSceneDescriptor? registeredScene = null;
        if (!string.IsNullOrWhiteSpace(activeSceneName) && runtimeCatalog.TryGetBySceneName(activeSceneName, out var resolvedScene))
        {
            registeredScene = resolvedScene;
            snapshot.HasSceneRegistration = resolvedScene is not null;
            snapshot.RegisteredSceneLogicalId = resolvedScene?.LogicalId ?? string.Empty;
        }

        LongLiveSceneLocalTopologyDescriptor? topology = null;
        if (!string.IsNullOrWhiteSpace(activeSceneName))
        {
            topologyCatalog.TryGetTopologyBySceneName(activeSceneName, out topology);
        }

        if (topology is null && registeredScene is not null && !string.IsNullOrWhiteSpace(registeredScene.LogicalId))
        {
            topologyCatalog.TryGetTopologyBySceneLogicalId(registeredScene.LogicalId, out topology);
        }

        if (topology is null)
        {
            return snapshot;
        }

        var nodes = topologyCatalog.GetNodesForTopology(topology.LogicalId);
        snapshot.HasActiveTopology = true;
        snapshot.TopologyLogicalId = topology.LogicalId;
        snapshot.TopologyDisplayName = topology.DisplayName;
        snapshot.ActiveTopologyNodeCount = nodes.Count;

        var activeNodeNames = new List<string>();
        foreach (var node in nodes)
        {
            if (activeNodeNames.Count >= sampleLimit)
            {
                break;
            }

            activeNodeNames.Add(node.DisplayName);
        }

        snapshot.ActiveNodeNames = activeNodeNames;
        return snapshot;
    }

    public static void LogInstallerSummary()
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[SceneLocalTopology] ready: topologies={snapshot.TotalTopologyCount}, nodes={snapshot.TotalNodeCount}, activeScene={snapshot.ActiveSceneName}, activeTopology={(snapshot.HasActiveTopology ? snapshot.TopologyLogicalId : "n/a")}");
        LogVerboseSnapshot(snapshot, "installer");
    }

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[SceneLocalTopology] sceneLoaded: scene={scene.name}, mode={mode}, registeredScene={snapshot.RegisteredSceneLogicalId}, activeTopology={(snapshot.HasActiveTopology ? snapshot.TopologyLogicalId : "n/a")}, nodeCount={snapshot.ActiveTopologyNodeCount}");
        LogVerboseSnapshot(snapshot, "sceneLoaded");
    }

    private static void LogVerboseSnapshot(LongLiveSceneLocalTopologyRuntimeSnapshot snapshot, string source)
    {
        if (!IsVerbose())
        {
            return;
        }

        var nodeSummary = snapshot.ActiveNodeNames.Count == 0
            ? "n/a"
            : string.Join(", ", snapshot.ActiveNodeNames.ToArray());
        _logger?.LogInfo($"[SceneLocalTopology] {source} detail: activeScene={snapshot.ActiveSceneName}, hasSceneRegistration={snapshot.HasSceneRegistration}, topologyDisplayName={snapshot.TopologyDisplayName}, activeNodes={nodeSummary}");
    }

    private static bool IsEnabled()
    {
        return _logger is not null
            && _options?.EnableDebugLogging.Value == true
            && _options.EnableSceneLocalTopologyLogging.Value;
    }

    private static bool IsVerbose()
    {
        return IsEnabled() && _options?.EnableSceneLocalTopologyVerbose.Value == true;
    }

    private static string SafeGetCurrentSceneName()
    {
        try
        {
            return SceneEx.NowSceneName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
