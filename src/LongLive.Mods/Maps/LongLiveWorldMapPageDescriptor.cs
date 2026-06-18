using System.Collections.Generic;

namespace LongLive.Mods.Maps;

public sealed class LongLiveWorldMapPageDescriptor
{
    public string LogicalId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string BackgroundAssetId { get; set; } = string.Empty;

    public string TabIconAssetId { get; set; } = string.Empty;

    public int? OrderHint { get; set; }

    public List<string> HighlightRegionIds { get; set; } = new List<string>();

    public List<string> NodeIds { get; set; } = new List<string>();
}
