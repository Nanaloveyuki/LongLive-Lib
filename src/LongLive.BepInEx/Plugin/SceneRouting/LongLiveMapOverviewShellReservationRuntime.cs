using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapOverviewShellReservationRuntime
{
    private static readonly Dictionary<string, GameObject> ReservedShells = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    private static ManualLogSource? _logger;
    private static LongLiveHostOptions? _options;

    public static void Initialize(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public static void EnsureReservedShells()
    {
        var allocationRuntime = LongLivePluginContext.GetMapOverviewShellAllocationRuntimeSnapshot();
        var panel = UIMapPanel.Inst;
        if (panel is null)
        {
            return;
        }

        foreach (var target in allocationRuntime.Targets)
        {
            if (!target.RequiresDedicatedShell || !target.CanBindInCurrentSession)
            {
                continue;
            }

            if (ReservedShells.ContainsKey(target.PageId))
            {
                continue;
            }

            var sourceRoot = ResolveSourceRoot(panel, target.HostSurface);
            if (sourceRoot is null)
            {
                continue;
            }

            var clone = UnityEngine.Object.Instantiate(sourceRoot.gameObject, sourceRoot.parent, false);
            clone.name = "LongLiveReservedShell_" + target.PageId;
            clone.SetActive(false);
            ReservedShells[target.PageId] = clone;
        }
    }

    public static LongLiveMapOverviewShellReservationRuntimeSnapshot CaptureSnapshot(int sampleLimit = 8)
    {
        EnsureReservedShells();

        var allocationRuntime = LongLivePluginContext.GetMapOverviewShellAllocationRuntimeSnapshot(int.MaxValue);
        var panel = UIMapPanel.Inst;
        var targets = new List<LongLiveMapOverviewShellReservationTarget>();
        foreach (var target in allocationRuntime.Targets)
        {
            if (!target.RequiresDedicatedShell)
            {
                continue;
            }

            ReservedShells.TryGetValue(target.PageId, out var reservedObject);
            var sourceRoot = ResolveSourceRoot(panel, target.HostSurface);
            targets.Add(new LongLiveMapOverviewShellReservationTarget
            {
                PageId = target.PageId,
                OwningModId = target.OwningModId,
                HostSurface = target.HostSurface,
                ReservedObjectName = reservedObject?.name ?? string.Empty,
                SourceRootName = sourceRoot?.name ?? string.Empty,
                ReservationCreated = reservedObject is not null,
                ReservationReusable = reservedObject is not null,
                ReservationHidden = reservedObject is not null && !reservedObject.activeSelf,
                StatusCode = ResolveStatusCode(target, reservedObject, sourceRoot),
            });
        }

        if (sampleLimit < targets.Count)
        {
            targets = targets.Take(sampleLimit).ToList();
        }

        return new LongLiveMapOverviewShellReservationRuntimeSnapshot
        {
            ActiveSceneName = allocationRuntime.ActiveSceneName,
            RequestedReservationCount = allocationRuntime.DedicatedShellCount,
            CreatedReservationCount = targets.Count(static target => target.ReservationCreated),
            ReusableReservationCount = targets.Count(static target => target.ReservationReusable),
            HiddenReservationCount = targets.Count(static target => target.ReservationHidden),
            Targets = targets,
        };
    }

    public static void LogInstallerSummary()
    {
        if (!IsEnabled())
        {
            return;
        }

        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[MapOverviewShellReservation] ready: requested={snapshot.RequestedReservationCount}, created={snapshot.CreatedReservationCount}, reusable={snapshot.ReusableReservationCount}, hidden={snapshot.HiddenReservationCount}");
        LogVerboseSnapshot(snapshot, "installer");
    }

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnabled())
        {
            return;
        }

        PruneDestroyedReservations();
        var snapshot = CaptureSnapshot();
        _logger?.LogInfo($"[MapOverviewShellReservation] sceneLoaded: scene={scene.name}, mode={mode}, requested={snapshot.RequestedReservationCount}, created={snapshot.CreatedReservationCount}, hidden={snapshot.HiddenReservationCount}");
        LogVerboseSnapshot(snapshot, "sceneLoaded");
    }

    private static void LogVerboseSnapshot(LongLiveMapOverviewShellReservationRuntimeSnapshot snapshot, string source)
    {
        if (!IsVerbose())
        {
            return;
        }

        var targets = snapshot.Targets.Count == 0
            ? "n/a"
            : string.Join(" | ", snapshot.Targets.Select(static target => target.PageId + ":status=" + target.StatusCode + ",reserved=" + (string.IsNullOrWhiteSpace(target.ReservedObjectName) ? "n/a" : target.ReservedObjectName)).ToArray());
        _logger?.LogInfo($"[MapOverviewShellReservation] {source} detail: activeScene={snapshot.ActiveSceneName}, targets={targets}");
    }

    private static Transform? ResolveSourceRoot(UIMapPanel? panel, string hostSurface)
    {
        if (panel is null)
        {
            return null;
        }

        if (string.Equals(hostSurface, "Sea", StringComparison.Ordinal))
        {
            return panel.Sea?.NodesRoot;
        }

        return panel.NingZhou?.NodesRoot;
    }

    private static string ResolveStatusCode(LongLiveMapOverviewShellAllocationTarget allocationTarget, GameObject? reservedObject, Transform? sourceRoot)
    {
        if (!allocationTarget.CanBindInCurrentSession)
        {
            return "binding-unavailable";
        }

        if (sourceRoot is null)
        {
            return "source-root-missing";
        }

        if (reservedObject is null)
        {
            return "reservation-missing";
        }

        return reservedObject.activeSelf ? "reservation-visible" : "reservation-hidden";
    }

    private static void PruneDestroyedReservations()
    {
        var removedKeys = new List<string>();
        foreach (var pair in ReservedShells)
        {
            if (pair.Value == null)
            {
                removedKeys.Add(pair.Key);
            }
        }

        foreach (var key in removedKeys)
        {
            ReservedShells.Remove(key);
        }
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
