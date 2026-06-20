namespace LongLive.Mods.Compatibility;

public sealed class LongLiveCompatibilityActivationRecord
{
    public string RedirectId { get; set; } = string.Empty;

    public string SourceLibraryId { get; set; } = string.Empty;

    public bool SourceDetected { get; set; }

    public bool RedirectEnabled { get; set; }

    public bool RedirectApplied { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public int InvocationCount { get; set; }

    public string LastInvocationStatusCode { get; set; } = string.Empty;

    public string LastInvocationDetail { get; set; } = string.Empty;
}
