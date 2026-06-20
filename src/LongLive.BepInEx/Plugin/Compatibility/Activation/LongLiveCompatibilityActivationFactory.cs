using LongLive.Mods.Compatibility;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCompatibilityActivationFactory
{
    public static LongLiveCompatibilityActivationRecord Create(
        string redirectId,
        string sourceLibraryId,
        bool sourceDetected,
        bool redirectEnabled,
        bool redirectApplied,
        string statusCode,
        string detail)
    {
        return new LongLiveCompatibilityActivationRecord
        {
            RedirectId = redirectId ?? string.Empty,
            SourceLibraryId = sourceLibraryId ?? string.Empty,
            SourceDetected = sourceDetected,
            RedirectEnabled = redirectEnabled,
            RedirectApplied = redirectApplied,
            StatusCode = statusCode ?? string.Empty,
            Detail = detail ?? string.Empty,
        };
    }
}
