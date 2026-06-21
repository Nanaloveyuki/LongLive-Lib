using System;
using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveMapOverviewCatalog : ILongLiveMapOverviewCatalog
{
    private readonly Dictionary<string, LongLiveWorldMapPageDescriptor> _pages = new Dictionary<string, LongLiveWorldMapPageDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveHighlightRegionDescriptor> _regions = new Dictionary<string, LongLiveHighlightRegionDescriptor>(StringComparer.Ordinal);
    private readonly Dictionary<string, LongLiveWorldNodeDescriptor> _nodes = new Dictionary<string, LongLiveWorldNodeDescriptor>(StringComparer.Ordinal);

    public IReadOnlyCollection<LongLiveWorldMapPageDescriptor> Pages => _pages.Values;

    public IReadOnlyCollection<LongLiveHighlightRegionDescriptor> Regions => _regions.Values;

    public IReadOnlyCollection<LongLiveWorldNodeDescriptor> Nodes => _nodes.Values;

    public int PageCount => _pages.Count;

    public int RegionCount => _regions.Count;

    public int NodeCount => _nodes.Count;

    public void RegisterPage(LongLiveWorldMapPageDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        _pages[descriptor.LogicalId] = descriptor;
    }

    public void RegisterRegion(LongLiveHighlightRegionDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        _regions[descriptor.LogicalId] = descriptor;
    }

    public void RegisterNode(LongLiveWorldNodeDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        _nodes[descriptor.LogicalId] = descriptor;
    }

    public bool TryGetPage(string logicalId, out LongLiveWorldMapPageDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(logicalId))
        {
            descriptor = null;
            return false;
        }

        return _pages.TryGetValue(logicalId, out descriptor);
    }

    public bool TryGetRegion(string logicalId, out LongLiveHighlightRegionDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(logicalId))
        {
            descriptor = null;
            return false;
        }

        return _regions.TryGetValue(logicalId, out descriptor);
    }

    public bool TryGetNode(string logicalId, out LongLiveWorldNodeDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(logicalId))
        {
            descriptor = null;
            return false;
        }

        return _nodes.TryGetValue(logicalId, out descriptor);
    }

    public IReadOnlyList<LongLiveWorldMapPageDescriptor> GetPagesForMod(string owningModId)
    {
        return FilterPages(page => string.Equals(page.OwningModId, owningModId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveHighlightRegionDescriptor> GetRegionsForMod(string owningModId)
    {
        return FilterRegions(region => string.Equals(region.OwningModId, owningModId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveWorldNodeDescriptor> GetNodesForMod(string owningModId)
    {
        return FilterNodes(node => string.Equals(node.OwningModId, owningModId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveHighlightRegionDescriptor> GetRegionsForPage(string pageId)
    {
        return FilterRegions(region => string.Equals(region.PageId, pageId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveWorldNodeDescriptor> GetNodesForPage(string pageId)
    {
        return FilterNodes(node => string.Equals(node.PageId, pageId, StringComparison.Ordinal));
    }

    public IReadOnlyList<LongLiveWorldNodeDescriptor> GetNodesForRegion(string regionId)
    {
        var nodes = new List<LongLiveWorldNodeDescriptor>();
        foreach (var region in _regions.Values)
        {
            if (!string.Equals(region.LogicalId, regionId, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var node in _nodes.Values)
            {
                if (string.Equals(node.PageId, region.PageId, StringComparison.Ordinal))
                {
                    nodes.Add(node);
                }
            }

            break;
        }

        return nodes;
    }

    private IReadOnlyList<LongLiveWorldMapPageDescriptor> FilterPages(Func<LongLiveWorldMapPageDescriptor, bool> predicate)
    {
        var results = new List<LongLiveWorldMapPageDescriptor>();
        foreach (var page in _pages.Values)
        {
            if (predicate(page))
            {
                results.Add(page);
            }
        }

        return results;
    }

    private IReadOnlyList<LongLiveHighlightRegionDescriptor> FilterRegions(Func<LongLiveHighlightRegionDescriptor, bool> predicate)
    {
        var results = new List<LongLiveHighlightRegionDescriptor>();
        foreach (var region in _regions.Values)
        {
            if (predicate(region))
            {
                results.Add(region);
            }
        }

        return results;
    }

    private IReadOnlyList<LongLiveWorldNodeDescriptor> FilterNodes(Func<LongLiveWorldNodeDescriptor, bool> predicate)
    {
        var results = new List<LongLiveWorldNodeDescriptor>();
        foreach (var node in _nodes.Values)
        {
            if (predicate(node))
            {
                results.Add(node);
            }
        }

        return results;
    }
}
