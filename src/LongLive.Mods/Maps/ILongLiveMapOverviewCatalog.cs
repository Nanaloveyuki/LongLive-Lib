using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public interface ILongLiveMapOverviewCatalog
{
    IReadOnlyCollection<LongLiveWorldMapPageDescriptor> Pages { get; }

    IReadOnlyCollection<LongLiveHighlightRegionDescriptor> Regions { get; }

    IReadOnlyCollection<LongLiveWorldNodeDescriptor> Nodes { get; }

    int PageCount { get; }

    int RegionCount { get; }

    int NodeCount { get; }

    bool TryGetPage(string logicalId, out LongLiveWorldMapPageDescriptor? descriptor);

    bool TryGetRegion(string logicalId, out LongLiveHighlightRegionDescriptor? descriptor);

    bool TryGetNode(string logicalId, out LongLiveWorldNodeDescriptor? descriptor);

    IReadOnlyList<LongLiveWorldMapPageDescriptor> GetPagesForMod(string owningModId);

    IReadOnlyList<LongLiveHighlightRegionDescriptor> GetRegionsForPage(string pageId);

    IReadOnlyList<LongLiveWorldNodeDescriptor> GetNodesForPage(string pageId);
}
