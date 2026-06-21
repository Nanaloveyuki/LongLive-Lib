using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using LongLive.Mods.Maps;
using LongLive.Next.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapOverviewUiInjectionRuntime
{
    private const string DemoNodeObjectName = "LongLiveDemoMapNode";

    private static readonly FieldInfo? NingZhouNodesField = AccessTools.Field(typeof(UIMapNingZhou), "nodes");
    private static readonly FieldInfo? NingZhouWarpNodesField = AccessTools.Field(typeof(UIMapNingZhou), "warpNodes");

    private static ManualLogSource? _logger;
    private static LongLiveHostOptions? _options;

    public static void Initialize(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnabled())
        {
            return;
        }

        _logger?.LogInfo($"[MapOverviewUiInjection] sceneLoaded: scene={scene.name}, mode={mode}");
    }

    public static void EnsureInjectedNingZhouNodes(UIMapNingZhou? panel, string source)
    {
        if (!ShouldRun() || panel?.NodesRoot is null)
        {
            return;
        }

        var injectedCount = 0;
        foreach (var projection in ResolveNingZhouProjections())
        {
            if (EnsureInjectedNode(panel, projection))
            {
                injectedCount++;
            }
        }

        RefreshInjectedNodePresentation(panel);

        if (TryFindMarker(panel.NodesRoot, LongLiveMapDemoConstants.WorldNodeId, out var marker) && marker is not null)
        {
            var summary = $"visible=true, host=NingZhou, scene={marker.SceneName}, injectedCount={CountInjectedNodes(panel.NodesRoot)}";
            LongLivePluginContext.GetRuntime().SetString(LongLiveMapDemoStateKeys.OverviewNodeStatus, summary);
        }

        if (IsVerbose())
        {
            _logger?.LogInfo($"[MapOverviewUiInjection] ensure: source={source}, injectedNow={injectedCount}, totalInjected={CountInjectedNodes(panel.NodesRoot)}");
        }
    }

    public static bool TryHandleInjectedNodeClick(UIMapNingZhouNode? node)
    {
        if (!ShouldRun() || node is null)
        {
            return false;
        }

        var marker = node.GetComponent<LongLiveMapOverviewUiNodeMarker>();
        if (marker is null)
        {
            return false;
        }

        string status;
        if (string.Equals(marker.NodeLogicalId, LongLiveMapDemoConstants.WorldNodeId, StringComparison.Ordinal))
        {
            status = new LongLiveMapDemoRuntimeService(LongLivePluginContext.GetLogger(), LongLivePluginContext.GetRuntime()).EnterDemoRuntime();
        }
        else if (LongLivePluginContext.MapOverview.Routing.TryCreateSceneAddressForNode(marker.NodeLogicalId, out var address) && address is not null)
        {
            var result = LongLivePluginContext.SceneRouting.WarpPlayer(address);
            status = result.Succeeded
                ? $"enter: success, scene={result.RequestedSceneName}, kind={result.RequestedSceneKind}, appliedEntry={(result.AppliedEntryIndex.HasValue ? result.AppliedEntryIndex.Value.ToString() : "n/a")}, detail={result.Detail}"
                : $"enter: failed, scene={result.RequestedSceneName}, kind={result.RequestedSceneKind}, code={result.FailureCode}, detail={result.Detail}";
        }
        else
        {
            status = $"enter: failed, scene={marker.SceneName}, logicalId={marker.SceneLogicalId}, code=projection-missing, detail=No route projection could be resolved for the injected overview node.";
        }

        LongLivePluginContext.GetRuntime().SetString(LongLiveMapDemoStateKeys.OverviewNodeStatus, $"clicked=true, node={marker.NodeLogicalId}, scene={marker.SceneName}");

        if (IsEnabled())
        {
            _logger?.LogInfo($"[MapOverviewUiInjection] click: node={marker.NodeLogicalId}, status={status}");
        }

        if (UIMapPanel.Inst is not null)
        {
            UIMapPanel.Inst.HidePanel();
        }

        return true;
    }

    private static IEnumerable<LongLiveMapOverviewRouteProjection> ResolveNingZhouProjections()
    {
        var installPlan = LongLivePluginContext.GetMapOverviewInstallPlan();
        var routing = LongLivePluginContext.MapOverview.Routing;

        foreach (var pageTarget in installPlan.PageTargets)
        {
            if (!pageTarget.RequiresHostInjection)
            {
                continue;
            }

            if (pageTarget.PageId.IndexOf("sea", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            foreach (var projection in routing.GetByPageId(pageTarget.PageId))
            {
                yield return projection;
            }
        }
    }

    private static bool EnsureInjectedNode(UIMapNingZhou panel, LongLiveMapOverviewRouteProjection projection)
    {
        if (TryFindMarker(panel.NodesRoot, projection.NodeLogicalId, out var existingMarker) && existingMarker is not null)
        {
            RefreshInjectedNode(existingMarker.GetComponent<UIMapNingZhouNode>(), existingMarker, projection);
            AddNodeToPrivateLists(panel, existingMarker.GetComponent<UIMapNingZhouNode>());
            return false;
        }

        var template = ResolveTemplateNode(panel.NodesRoot);
        if (template is null)
        {
            if (IsEnabled())
            {
                _logger?.LogWarning($"[MapOverviewUiInjection] template missing for projection node={projection.NodeLogicalId}");
            }

            return false;
        }

        var clone = UnityEngine.Object.Instantiate(template.gameObject, panel.NodesRoot, false);
        clone.name = DemoNodeObjectName + "_" + projection.NodeLogicalId;
        clone.SetActive(true);

        var injectedNode = clone.GetComponent<UIMapNingZhouNode>();
        if (injectedNode is null)
        {
            UnityEngine.Object.Destroy(clone);
            return false;
        }

        injectedNode.Init();
        var marker = clone.GetComponent<LongLiveMapOverviewUiNodeMarker>() ?? clone.AddComponent<LongLiveMapOverviewUiNodeMarker>();
        RefreshInjectedNode(injectedNode, marker, projection);
        AddNodeToPrivateLists(panel, injectedNode);
        return true;
    }

    private static void RefreshInjectedNode(UIMapNingZhouNode? node, LongLiveMapOverviewUiNodeMarker marker, LongLiveMapOverviewRouteProjection projection)
    {
        if (node is null)
        {
            return;
        }

        node.SetNodeName(string.IsNullOrWhiteSpace(projection.NodeDisplayName) ? projection.SceneDisplayName : projection.NodeDisplayName);
        node.WarpSceneName = projection.SceneName;

        marker.NodeLogicalId = projection.NodeLogicalId;
        marker.PageId = projection.PageId;
        marker.SceneLogicalId = projection.SceneLogicalId;
        marker.SceneName = projection.SceneName;

        var rect = node.transform as RectTransform;
        if (rect is not null)
        {
            rect.anchoredPosition = ResolveAnchoredPosition(projection);
            rect.localScale = Vector3.one;
        }
    }

    private static Vector2 ResolveAnchoredPosition(LongLiveMapOverviewRouteProjection projection)
    {
        if (LongLivePluginContext.MapOverview.Catalog.TryGetNode(projection.NodeLogicalId, out var descriptor) && descriptor is not null)
        {
            var x = descriptor.Position.X;
            var y = descriptor.Position.Y;

            if (UIMapPanel.Inst?.NingZhou?.NodesRoot is RectTransform rootRect)
            {
                var maxX = Math.Max(120f, rootRect.rect.width * 0.45f);
                var maxY = Math.Max(120f, rootRect.rect.height * 0.45f);
                x = Mathf.Clamp(x, -maxX, maxX);
                y = Mathf.Clamp(y, -maxY, maxY);
            }

            return new Vector2(x, y);
        }

        return new Vector2(320f, 160f);
    }

    private static UIMapNingZhouNode? ResolveTemplateNode(Transform nodesRoot)
    {
        return nodesRoot.GetComponentsInChildren<UIMapNingZhouNode>(true)
            .FirstOrDefault(static node => node.GetComponent<LongLiveMapOverviewUiNodeMarker>() is null);
    }

    private static void RefreshInjectedNodePresentation(UIMapNingZhou panel)
    {
        foreach (var marker in panel.NodesRoot.GetComponentsInChildren<LongLiveMapOverviewUiNodeMarker>(true))
        {
            var node = marker.GetComponent<UIMapNingZhouNode>();
            if (node is null)
            {
                continue;
            }

            node.gameObject.SetActive(true);
            node.SetNodeAlpha(false);
            node.SetCanJiaoHu(true);
        }
    }

    private static void AddNodeToPrivateLists(UIMapNingZhou panel, UIMapNingZhouNode? node)
    {
        if (node is null)
        {
            return;
        }

        if (NingZhouNodesField?.GetValue(panel) is IList nodes && !ContainsReference(nodes, node))
        {
            nodes.Add(node);
        }

        if (NingZhouWarpNodesField?.GetValue(panel) is IList warpNodes && !ContainsReference(warpNodes, node))
        {
            warpNodes.Add(node);
        }
    }

    private static bool ContainsReference(IList list, object item)
    {
        foreach (var entry in list)
        {
            if (ReferenceEquals(entry, item))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindMarker(Transform root, string nodeLogicalId, out LongLiveMapOverviewUiNodeMarker? marker)
    {
        marker = root.GetComponentsInChildren<LongLiveMapOverviewUiNodeMarker>(true)
            .FirstOrDefault(candidate => string.Equals(candidate.NodeLogicalId, nodeLogicalId, StringComparison.Ordinal));
        return marker is not null;
    }

    private static int CountInjectedNodes(Transform root)
    {
        return root.GetComponentsInChildren<LongLiveMapOverviewUiNodeMarker>(true).Length;
    }

    private static bool ShouldRun()
    {
        return LongLivePlugin.Instance is not null
            && LongLivePlugin.Instance.Options.EnableDemoMapRegistration.Value;
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
