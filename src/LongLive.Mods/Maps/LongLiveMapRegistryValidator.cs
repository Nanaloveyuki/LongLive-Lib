using System;
using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveMapRegistryValidator
{
    public LongLiveMapRegistryValidationResult Validate(LongLiveMapRegistryDraft draft)
    {
        if (draft is null)
        {
            throw new ArgumentNullException(nameof(draft));
        }

        var result = new LongLiveMapRegistryValidationResult();
        var pageIds = new HashSet<string>(StringComparer.Ordinal);
        var sceneLogicalIds = new HashSet<string>(StringComparer.Ordinal);
        var sceneNames = new HashSet<string>(StringComparer.Ordinal);
        var highlightIds = new HashSet<string>(StringComparer.Ordinal);
        var nodeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var page in draft.Pages)
        {
            ValidateLogicalId(page.LogicalId, "page", result);
            if (!string.IsNullOrWhiteSpace(page.LogicalId) && !pageIds.Add(page.LogicalId))
            {
                result.AddError("duplicate-page-id", $"Duplicate world-map page logical ID: {page.LogicalId}");
            }
        }

        foreach (var scene in draft.Scenes)
        {
            ValidateLogicalId(scene.LogicalId, "scene", result);
            if (!string.IsNullOrWhiteSpace(scene.LogicalId) && !sceneLogicalIds.Add(scene.LogicalId))
            {
                result.AddError("duplicate-scene-id", $"Duplicate scene logical ID: {scene.LogicalId}");
            }

            if (!string.IsNullOrWhiteSpace(scene.SceneName) && !sceneNames.Add(scene.SceneName))
            {
                result.AddError("duplicate-scene-name", $"Duplicate host scene name: {scene.SceneName}");
            }

            if (!string.IsNullOrWhiteSpace(scene.OverviewPageId) && !pageIds.Contains(scene.OverviewPageId))
            {
                result.AddError("missing-scene-page", $"Scene {scene.LogicalId} references missing page {scene.OverviewPageId}");
            }
        }

        foreach (var region in draft.HighlightRegions)
        {
            ValidateLogicalId(region.LogicalId, "highlight region", result);
            if (!string.IsNullOrWhiteSpace(region.LogicalId) && !highlightIds.Add(region.LogicalId))
            {
                result.AddError("duplicate-highlight-id", $"Duplicate highlight region logical ID: {region.LogicalId}");
            }

            if (!string.IsNullOrWhiteSpace(region.PageId) && !pageIds.Contains(region.PageId))
            {
                result.AddError("missing-highlight-page", $"Highlight region {region.LogicalId} references missing page {region.PageId}");
            }
        }

        foreach (var node in draft.Nodes)
        {
            ValidateLogicalId(node.LogicalId, "node", result);
            if (!string.IsNullOrWhiteSpace(node.LogicalId) && !nodeIds.Add(node.LogicalId))
            {
                result.AddError("duplicate-node-id", $"Duplicate node logical ID: {node.LogicalId}");
            }

            if (!string.IsNullOrWhiteSpace(node.PageId) && !pageIds.Contains(node.PageId))
            {
                result.AddError("missing-node-page", $"Node {node.LogicalId} references missing page {node.PageId}");
            }

            if (!string.IsNullOrWhiteSpace(node.TargetSceneLogicalId) && !sceneLogicalIds.Contains(node.TargetSceneLogicalId))
            {
                result.AddError("missing-node-scene", $"Node {node.LogicalId} references missing target scene {node.TargetSceneLogicalId}");
            }
        }

        foreach (var node in draft.Nodes)
        {
            foreach (var connectedNodeId in node.ConnectedNodeIds)
            {
                if (!string.IsNullOrWhiteSpace(connectedNodeId) && !nodeIds.Contains(connectedNodeId))
                {
                    result.AddError("missing-node-connection", $"Node {node.LogicalId} references missing connected node {connectedNodeId}");
                }
            }
        }

        return result;
    }

    private static void ValidateLogicalId(string logicalId, string kind, LongLiveMapRegistryValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(logicalId))
        {
            result.AddError("missing-logical-id", $"A {kind} entry is missing its logical ID.");
            return;
        }

        if (logicalId.IndexOf('.') < 0)
        {
            result.AddWarning("logical-id-without-namespace", $"{kind} logical ID should be namespaced: {logicalId}");
        }
    }
}
