using System;
using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveMapRegistryPlanner
{
    private readonly LongLiveMapRegistryValidator _validator;

    public LongLiveMapRegistryPlanner(LongLiveMapRegistryValidator? validator = null)
    {
        _validator = validator ?? new LongLiveMapRegistryValidator();
    }

    public LongLiveMapRegistryPlan CreatePlan(LongLiveMapRegistryDraft draft, LongLiveMapHostAllocationRanges? ranges = null)
    {
        if (draft is null)
        {
            throw new ArgumentNullException(nameof(draft));
        }

        var validation = _validator.Validate(draft);
        var allocationRegistry = new LongLiveMapAllocationRegistry(ranges);
        var ownership = BuildOwnershipMap(draft);

        if (validation.IsValid)
        {
            foreach (var scene in draft.Scenes)
            {
                if (scene.HostMapType is null)
                {
                    scene.HostMapType = allocationRegistry.AllocateMapType(scene.LogicalId);
                }

                if (!string.IsNullOrWhiteSpace(scene.HighlightRegionId) && scene.HostHighlightId is null)
                {
                    scene.HostHighlightId = allocationRegistry.AllocateHighlightId(scene.HighlightRegionId);
                }

                if (!string.IsNullOrWhiteSpace(scene.OutsideSceneLogicalId) && scene.HostOutsideScenePos is null)
                {
                    scene.HostOutsideScenePos = allocationRegistry.AllocateOutsideScenePos(scene.LogicalId + ".outside-pos");
                }
            }

            foreach (var region in draft.HighlightRegions)
            {
                if (region.HostHighlightId is null)
                {
                    region.HostHighlightId = allocationRegistry.AllocateHighlightId(region.LogicalId);
                }
            }

            foreach (var node in draft.Nodes)
            {
                if (node.HostNodeIndex is null)
                {
                    node.HostNodeIndex = allocationRegistry.AllocateNodeIndex(node.LogicalId);
                }
            }
        }

        return new LongLiveMapRegistryPlan(
            draft,
            validation,
            allocationRegistry.BuildReport(ownership));
    }

    private static Dictionary<string, string> BuildOwnershipMap(LongLiveMapRegistryDraft draft)
    {
        var ownership = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var page in draft.Pages)
        {
            ownership[page.LogicalId] = page.OwningModId;
        }

        foreach (var scene in draft.Scenes)
        {
            ownership[scene.LogicalId] = scene.OwningModId;
        }

        foreach (var region in draft.HighlightRegions)
        {
            ownership[region.LogicalId] = region.OwningModId;
        }

        foreach (var node in draft.Nodes)
        {
            ownership[node.LogicalId] = node.OwningModId;
        }

        return ownership;
    }
}
