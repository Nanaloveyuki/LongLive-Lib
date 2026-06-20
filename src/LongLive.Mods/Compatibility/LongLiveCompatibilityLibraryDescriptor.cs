namespace LongLive.Mods.Compatibility;

public sealed class LongLiveCompatibilityLibraryDescriptor
{
    public string LibraryId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public LongLiveCompatibilityRelationshipMode RelationshipMode { get; set; } = LongLiveCompatibilityRelationshipMode.ReferenceOnly;

    public string CapabilityFamily { get; set; } = string.Empty;

    public string DetectionTypeName { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}
