namespace LongLive.Mods.Maps;

public sealed class LongLiveHighlightRegionDescriptor
{
    public string LogicalId { get; set; } = string.Empty;

    public string OwningModId { get; set; } = string.Empty;

    public string PageId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int? HostHighlightId { get; set; }
}
