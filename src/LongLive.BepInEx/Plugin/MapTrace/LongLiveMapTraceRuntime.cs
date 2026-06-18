using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using JSONClass;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapTraceRuntime
{
    private static readonly HashSet<string> ReportedPatchNames = new HashSet<string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> LastSnapshotByKey = new Dictionary<string, string>(StringComparer.Ordinal);

    public static bool IsEnabled => LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true
        && LongLivePlugin.Instance?.Options.EnableMapTrace.Value == true;

    public static bool IsVerbose => LongLivePlugin.Instance?.Options.EnableMapTraceVerbose.Value == true;

    private static ManualLogSource? Logger => LongLivePlugin.LogSource;

    public static bool Prepare(string patchName)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (ReportedPatchNames.Add(patchName))
        {
            Log($"patch prepared: {patchName}");
        }

        return true;
    }

    public static void Log(string message)
    {
        Logger?.LogInfo($"[MapTrace] {message}");
    }

    public static void LogVerbose(string message)
    {
        if (IsVerbose)
        {
            Log(message);
        }
    }

    public static void OnUnitySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnabled)
        {
            return;
        }

        LogSceneSnapshot($"unity-sceneLoaded:{mode}", scene.name, force: true);
        LogSceneObjectAvailability($"unity-sceneLoaded:{mode}");

        if (AllMapManage.instance is not null)
        {
            LogAllMapManageSnapshot(AllMapManage.instance, $"unity-sceneLoaded:{mode}", force: true);
        }

        if (UIMapPanel.Inst is not null)
        {
            LogUiMapPanelSnapshot(UIMapPanel.Inst, $"unity-sceneLoaded:{mode}", force: true);
            LogUiMapNingZhouSnapshot(UIMapPanel.Inst.NingZhou, $"unity-sceneLoaded:{mode}", force: true);
            LogUiMapSeaSnapshot(UIMapPanel.Inst.Sea, $"unity-sceneLoaded:{mode}", force: true);
        }
    }

    public static string ActiveSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    public static void LogSceneLoadRequest(string requestedSceneName, bool lastSceneIsValue)
    {
        if (!IsEnabled)
        {
            return;
        }

        string? currentSceneName = null;
        string? playerLastScene = null;
        string? playerLastFuBenScene = null;
        int? playerNowMapIndex = null;

        try
        {
            currentSceneName = ActiveSceneName();
        }
        catch
        {
            currentSceneName = null;
        }

        try
        {
            if (PlayerEx.Player is not null)
            {
                playerLastScene = PlayerEx.Player.lastScence;
                playerLastFuBenScene = PlayerEx.Player.lastFuBenScence;
                playerNowMapIndex = PlayerEx.Player.NowMapIndex;
            }
        }
        catch
        {
        }

        Log(
            $"Tools.loadMapScenes prefix: request={requestedSceneName}, lastSceneIsValue={lastSceneIsValue}, currentScene={currentSceneName ?? "n/a"}, player.lastScence={playerLastScene ?? "n/a"}, player.lastFuBenScence={playerLastFuBenScene ?? "n/a"}, player.NowMapIndex={(playerNowMapIndex?.ToString() ?? "n/a")}");
        LogSceneMetadataForScene(requestedSceneName, "loadMapScenes.requested");
    }

    public static void LogSceneSnapshot(string source, string sceneName, bool force = false)
    {
        if (!IsEnabled)
        {
            return;
        }

        var summary = DescribeSceneSnapshot(sceneName);
        LogIfChanged($"scene:{sceneName}", $"{source}: {summary}", force);
    }

    public static void LogAllMapManageSnapshot(AllMapManage? instance, string source, bool force = false)
    {
        if (!IsEnabled || instance is null)
        {
            return;
        }

        var summary = DescribeAllMapManage(instance);
        LogIfChanged("allMapManage", $"{source}: {summary}", force);

        if (IsVerbose)
        {
            LogNodeSamples(instance, source, force);
        }
    }

    public static void LogNodeRegistration(BaseMapCompont? node, string source)
    {
        if (!IsEnabled || node is null)
        {
            return;
        }

        var summary = DescribeNode(node);
        LogIfChanged($"node:{node.GetType().FullName}:{node.NodeIndex}", $"{source}: {summary}", force: false);
    }

    public static void LogNodeMove(BaseMapCompont? node, string source)
    {
        if (!IsEnabled || node is null)
        {
            return;
        }

        Log($"{source}: {DescribeNode(node)}");
    }

    public static void LogUiMapPanelSnapshot(UIMapPanel? panel, string source, bool force = false)
    {
        if (!IsEnabled || panel is null)
        {
            return;
        }

        var summary = DescribeUiMapPanel(panel);
        LogIfChanged("uiMapPanel", $"{source}: {summary}", force);
    }

    public static void LogUiMapNingZhouSnapshot(UIMapNingZhou? panel, string source, bool force = false)
    {
        if (!IsEnabled || panel is null)
        {
            return;
        }

        var summary = DescribeUiMapNingZhou(panel);
        LogIfChanged("uiMapNingZhou", $"{source}: {summary}", force);
    }

    public static void LogUiMapSeaSnapshot(UIMapSea? panel, string source, bool force = false)
    {
        if (!IsEnabled || panel is null)
        {
            return;
        }

        var summary = DescribeUiMapSea(panel);
        LogIfChanged("uiMapSea", $"{source}: {summary}", force);
    }

    private static void LogIfChanged(string key, string message, bool force)
    {
        if (force || !LastSnapshotByKey.TryGetValue(key, out var previous) || !StringComparer.Ordinal.Equals(previous, message))
        {
            LastSnapshotByKey[key] = message;
            Log(message);
        }
    }

    private static string DescribeSceneSnapshot(string sceneName)
    {
        string area = "n/a";
        try
        {
            area = SceneEx.GetNowMapArea().ToString();
        }
        catch
        {
        }

        string playerState = "player=n/a";
        try
        {
            if (PlayerEx.Player is not null)
            {
                playerState = $"player.NowMapIndex={PlayerEx.Player.NowMapIndex}, player.lastScence={PlayerEx.Player.lastScence ?? "n/a"}, player.lastFuBenScence={PlayerEx.Player.lastFuBenScence ?? "n/a"}";
            }
        }
        catch
        {
        }

        return $"scene={sceneName}, area={area}, metadata={DescribeSceneMetadata(sceneName)}, {playerState}";
    }

    private static string DescribeSceneMetadata(string sceneName)
    {
        try
        {
            if (!SceneNameJsonData.DataDict.TryGetValue(sceneName, out var metadata) || metadata is null)
            {
                return "missing";
            }

            return string.Format(
                "MapType={0}, MoneyType={1}, HighlightID={2}, OutsideScenePos={3}, OutsideSceneName={4}, EventName={5}, MapName={6}",
                metadata.MapType,
                metadata.MoneyType,
                metadata.HighlightID,
                metadata.OutsideScenePos,
                metadata.OutsideSceneName ?? "",
                metadata.EventName ?? "",
                metadata.MapName ?? "");
        }
        catch (Exception exception)
        {
            return $"error:{exception.GetType().Name}";
        }
    }

    private static void LogSceneMetadataForScene(string sceneName, string source)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        LogIfChanged($"scene-metadata:{sceneName}", $"{source}: scene={sceneName}, metadata={DescribeSceneMetadata(sceneName)}", force: false);
    }

    private static string DescribeAllMapManage(AllMapManage instance)
    {
        int playerNowMapIndex = -1;
        try
        {
            if (PlayerEx.Player is not null)
            {
                playerNowMapIndex = PlayerEx.Player.NowMapIndex;
            }
        }
        catch
        {
        }

        var nodeTypeCounts = instance.mapIndex
            .Values
            .GroupBy(static node => node.GetType().Name)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .Select(static group => $"{group.Key}:{group.Count()}")
            .ToArray();

        return string.Format(
            "scene={0}, mapIndexCount={1}, player.NowMapIndex={2}, containsPlayerNode={3}, isPlayMove={4}, canLoad={5}, randomFlagCount={6}, luXianGroup={7}, allNodeGroup={8}, nodeTypes=[{9}]",
            ActiveSceneName(),
            instance.mapIndex.Count,
            playerNowMapIndex,
            instance.mapIndex.ContainsKey(playerNowMapIndex),
            instance.isPlayMove,
            instance.canLoad,
            instance.RandomFlag?.Count ?? 0,
            DescribeGameObject(instance.LuXianGroup),
            DescribeGameObject(instance.AllNodeGameobjGroup),
            string.Join(", ", nodeTypeCounts));
    }

    private static void LogNodeSamples(AllMapManage instance, string source, bool force)
    {
        var sampleNodes = instance.mapIndex
            .Values
            .OrderBy(static node => node.NodeIndex)
            .Take(8)
            .Select(DescribeNode)
            .ToArray();

        LogIfChanged(
            "allMapManage.nodeSamples",
            $"{source}: node-samples=[{string.Join(" | ", sampleNodes)}]",
            force);
    }

    private static string DescribeNode(BaseMapCompont node)
    {
        var nodeType = node.GetType().Name;
        var nextIndex = node.nextIndex is null || node.nextIndex.Count == 0
            ? "[]"
            : $"[{string.Join(",", node.nextIndex.OrderBy(static index => index))}]";
        var position = node.transform.position;
        var meta = DescribeNodeMetadata(node.NodeIndex);

        var suffix = string.Empty;
        if (node is MapComponent mapComponent)
        {
            suffix = $", NodeGroup={mapComponent.NodeGroup}";
        }
        else if (node is MapSeaCompent seaComponent)
        {
            suffix = $", MapPos={seaComponent.MapPositon}";
        }
        else if (node is MapInstComport)
        {
            suffix = ", kind=FuBenNode";
        }

        return string.Format(
            "nodeType={0}, nodeIndex={1}, name={2}, active={3}, isStatic={4}, next={5}, pos=({6:F1},{7:F1},{8:F1}), meta={9}{10}",
            nodeType,
            node.NodeIndex,
            node.name,
            node.gameObject.activeSelf,
            node.IsStatic,
            nextIndex,
            position.x,
            position.y,
            position.z,
            meta,
            suffix);
    }

    private static string DescribeNodeMetadata(int nodeIndex)
    {
        try
        {
            if (!AllMapLuDainType.DataDict.TryGetValue(nodeIndex, out var metadata) || metadata is null)
            {
                return "missing";
            }

            return $"MapType={metadata.MapType}, LuDianName={metadata.LuDianName ?? string.Empty}";
        }
        catch (Exception exception)
        {
            return $"error:{exception.GetType().Name}";
        }
    }

    private static string DescribeUiMapPanel(UIMapPanel panel)
    {
        return string.Format(
            "scene={0}, IsShow={1}, NowArea={2}, NowState={3}, NeedHighlightBlock={4}, panelObj={5}, tabRootActive={6}, bg={7}",
            ActiveSceneName(),
            panel.IsShow,
            panel.NowArea,
            panel.NowState,
            panel.NeedHighlightBlock,
            DescribeGameObject(panel.PanelObj),
            panel.TabRoot is not null && panel.TabRoot.activeSelf,
            panel.MapBG is not null && panel.MapBG.sprite is not null ? panel.MapBG.sprite.name : "null");
    }

    private static string DescribeUiMapNingZhou(UIMapNingZhou panel)
    {
        var highlightRoot = panel.HighlightBlockRoot;
        var nodesRoot = panel.NodesRoot;
        var highlightIds = highlightRoot is null
            ? Array.Empty<string>()
            : highlightRoot.GetComponentsInChildren<UIMapHighlight>(true).Select(static item => item.ID.ToString()).OrderBy(static item => item, StringComparer.Ordinal).ToArray();
        var warpNodes = nodesRoot is null
            ? Array.Empty<string>()
            : nodesRoot.GetComponentsInChildren<UIMapNingZhouNode>(true)
                .Where(static item => !string.IsNullOrWhiteSpace(item.WarpSceneName))
                .Select(static item => $"{item.NodeName}->{item.WarpSceneName}")
                .OrderBy(static item => item, StringComparer.Ordinal)
                .ToArray();

        return string.Format(
            "highlightCount={0}, nodeCount={1}, fungusNowScene={2}, bg={3}, highlights=[{4}], warpNodes=[{5}]",
            highlightIds.Length,
            nodesRoot is null ? 0 : nodesRoot.GetComponentsInChildren<UIMapNingZhouNode>(true).Length,
            panel.FungusNowScene ?? string.Empty,
            panel.BGSprite is not null ? panel.BGSprite.name : "null",
            string.Join(", ", highlightIds),
            string.Join(", ", warpNodes));
    }

    private static string DescribeUiMapSea(UIMapSea panel)
    {
        var highlightRoot = panel.HighlightBlockRoot;
        var namesRoot = panel.NamesRoot;
        var nodesRoot = panel.NodesRoot;
        var highlightIds = highlightRoot is null
            ? Array.Empty<string>()
            : highlightRoot.GetComponentsInChildren<UIMapHighlight>(true).Select(static item => item.ID.ToString()).OrderBy(static item => item, StringComparer.Ordinal).ToArray();
        var seaNames = namesRoot is null
            ? Array.Empty<string>()
            : namesRoot.GetComponentsInChildren<UIMapSeaName>(true)
                .Select(static item => $"SeaID={item.SeaID}:Highlight={item.BindHighlightID}")
                .OrderBy(static item => item, StringComparer.Ordinal)
                .ToArray();
        var warpNodes = nodesRoot is null
            ? Array.Empty<string>()
            : nodesRoot.GetComponentsInChildren<UIMapSeaNode>(true)
                .Select(static item => $"{item.NodeName}:{item.NodeIndex}->{item.WarpSceneName}")
                .OrderBy(static item => item, StringComparer.Ordinal)
                .ToArray();

        return string.Format(
            "highlightCount={0}, seaNameCount={1}, nodeCount={2}, bg={3}, highlights=[{4}], seaNames=[{5}], nodes=[{6}]",
            highlightIds.Length,
            seaNames.Length,
            warpNodes.Length,
            panel.BGSprite is not null ? panel.BGSprite.name : "null",
            string.Join(", ", highlightIds),
            string.Join(", ", seaNames),
            string.Join(", ", warpNodes));
    }

    private static string DescribeGameObject(GameObject? gameObject)
    {
        if (gameObject is null)
        {
            return "null";
        }

        return $"{gameObject.name}(active={gameObject.activeSelf})";
    }

    private static void LogSceneObjectAvailability(string source)
    {
        var allMapManage = AllMapManage.instance;
        var uiMapPanel = UIMapPanel.Inst;
        var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        var rootNames = rootObjects
            .Select(static gameObject => gameObject.name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .Take(16)
            .ToArray();

        var passiveMapPanels = Resources.FindObjectsOfTypeAll<UIMapPanel>();
        var passiveMapHighlights = Resources.FindObjectsOfTypeAll<UIMapHighlight>();
        var passiveImages = Resources.FindObjectsOfTypeAll<Image>();

        Log(
            $"{source}: scene-object-availability: AllMapManage={(allMapManage is not null)}, UIMapPanel={(uiMapPanel is not null)}, passiveUIMapPanels={passiveMapPanels.Length}, passiveHighlights={passiveMapHighlights.Length}, passiveImages={passiveImages.Length}, rootObjects={rootObjects.Length}, rootSample=[{string.Join(", ", rootNames)}]");
    }
}
