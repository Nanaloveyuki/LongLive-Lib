namespace LongLive.Mods.Compatibility;

public sealed class LongLiveCompatibilityRedirectDescriptor
{
    public string RedirectId { get; set; } = string.Empty;

    public string SourceLibraryId { get; set; } = string.Empty;

    public string CapabilityFamily { get; set; } = string.Empty;

    public string TargetSurface { get; set; } = string.Empty;

    public string DetectionTypeName { get; set; } = string.Empty;

    public string DetectionMethodName { get; set; } = string.Empty;

    public bool EnabledByDefault { get; set; } = true;

    public string Notes { get; set; } = string.Empty;
}
