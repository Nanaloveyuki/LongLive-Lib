using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveMapRegistryDraft
{
    public List<LongLiveSceneDescriptor> Scenes { get; set; } = new List<LongLiveSceneDescriptor>();

    public List<LongLiveWorldMapPageDescriptor> Pages { get; set; } = new List<LongLiveWorldMapPageDescriptor>();

    public List<LongLiveHighlightRegionDescriptor> HighlightRegions { get; set; } = new List<LongLiveHighlightRegionDescriptor>();

    public List<LongLiveWorldNodeDescriptor> Nodes { get; set; } = new List<LongLiveWorldNodeDescriptor>();
}
