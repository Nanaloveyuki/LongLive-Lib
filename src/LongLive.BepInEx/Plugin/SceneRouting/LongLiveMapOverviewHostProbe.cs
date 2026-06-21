using System;
using UnityEngine;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapOverviewHostProbe
{
    public static bool TryCapture(out LongLiveMapOverviewHostProbeSnapshot snapshot)
    {
        try
        {
            var panel = UIMapPanel.Inst;
            snapshot = new LongLiveMapOverviewHostProbeSnapshot
            {
                HasUiMapPanel = panel is not null,
                HasNingZhou = panel?.NingZhou is not null,
                HasSea = panel?.Sea is not null,
                PanelObjectName = panel?.name ?? string.Empty,
                TabRootObjectName = panel?.TabRoot?.name ?? string.Empty,
                MapBackgroundObjectName = panel?.MapBG?.name ?? string.Empty,
            };

            if (panel?.NingZhou is not null)
            {
                snapshot.NingZhouNodeRootName = panel.NingZhou.NodesRoot?.name ?? string.Empty;
                snapshot.NingZhouHighlightRootName = panel.NingZhou.HighlightBlockRoot?.name ?? string.Empty;
                snapshot.NingZhouNodeChildCount = panel.NingZhou.NodesRoot?.childCount ?? 0;
                snapshot.NingZhouHighlightChildCount = panel.NingZhou.HighlightBlockRoot?.childCount ?? 0;
                snapshot.NingZhouInjectionAnchorName = ResolveInjectionAnchorName(panel.NingZhou.NodesRoot, panel.NingZhou.HighlightBlockRoot, out var hasNingZhouAnchor);
                snapshot.HasNingZhouInjectionAnchor = hasNingZhouAnchor;
                snapshot.NingZhouHierarchySample = BuildHierarchySample(panel.NingZhou.NodesRoot, panel.NingZhou.HighlightBlockRoot);
            }

            if (panel?.Sea is not null)
            {
                snapshot.SeaNodeRootName = panel.Sea.NodesRoot?.name ?? string.Empty;
                snapshot.SeaHighlightRootName = panel.Sea.HighlightBlockRoot?.name ?? string.Empty;
                snapshot.SeaNamesRootName = panel.Sea.NamesRoot?.name ?? string.Empty;
                snapshot.SeaNodeChildCount = panel.Sea.NodesRoot?.childCount ?? 0;
                snapshot.SeaHighlightChildCount = panel.Sea.HighlightBlockRoot?.childCount ?? 0;
                snapshot.SeaNameChildCount = panel.Sea.NamesRoot?.childCount ?? 0;
                snapshot.SeaInjectionAnchorName = ResolveInjectionAnchorName(panel.Sea.NodesRoot, panel.Sea.HighlightBlockRoot, out var hasSeaAnchor);
                snapshot.HasSeaInjectionAnchor = hasSeaAnchor;
                snapshot.SeaHierarchySample = BuildHierarchySample(panel.Sea.NodesRoot, panel.Sea.HighlightBlockRoot, panel.Sea.NamesRoot);
            }

            return true;
        }
        catch (Exception exception)
        {
            snapshot = new LongLiveMapOverviewHostProbeSnapshot
            {
                ProbeError = exception.GetType().Name + ": " + exception.Message,
            };
            return false;
        }
    }

    private static string ResolveInjectionAnchorName(Transform? primaryRoot, Transform? secondaryRoot, out bool hasAnchor)
    {
        hasAnchor = false;

        var anchor = TryResolveAnchor(primaryRoot);
        if (anchor is not null)
        {
            hasAnchor = true;
            return anchor.name;
        }

        anchor = TryResolveAnchor(secondaryRoot);
        if (anchor is not null)
        {
            hasAnchor = true;
            return anchor.name;
        }

        return string.Empty;
    }

    private static Transform? TryResolveAnchor(Transform? root)
    {
        return root;
    }

    private static string BuildHierarchySample(params Transform?[] roots)
    {
        var samples = new System.Collections.Generic.List<string>();
        foreach (var root in roots)
        {
            if (root is null)
            {
                continue;
            }

            var line = root.name + "[" + root.childCount + "]";
            var sampleCount = Math.Min(root.childCount, 4);
            for (var index = 0; index < sampleCount; index++)
            {
                var child = root.GetChild(index);
                line += " -> " + child.name;
            }

            samples.Add(line);
        }

        return samples.Count == 0 ? string.Empty : string.Join(" | ", samples.ToArray());
    }
}
