using System;
using BepInEx.Logging;
using LongLive.Mods.Compatibility;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveEasyBatchCompatibilityFeature : ILongLiveCompatibilityFeature
{
    private readonly LongLiveCompatibilityRuntime _compatibilityRuntime;
    private readonly LongLiveEasyBatchUpdateRedirect _redirect;

    public LongLiveEasyBatchCompatibilityFeature(ManualLogSource logger, LongLiveCompatibilityRuntime compatibilityRuntime)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        _compatibilityRuntime = compatibilityRuntime ?? throw new ArgumentNullException(nameof(compatibilityRuntime));
        _redirect = new LongLiveEasyBatchUpdateRedirect(logger, compatibilityRuntime);
    }

    public LongLiveCompatibilityLibraryDescriptor Library => new LongLiveCompatibilityLibraryDescriptor
    {
        LibraryId = "easybatch",
        DisplayName = "EasyBatch",
        RelationshipMode = LongLiveCompatibilityRelationshipMode.ChokePointRedirect,
        CapabilityFamily = "bulk-item-use",
        DetectionTypeName = "EasyBatch.Plugin",
        Notes = "LongLive may redirect long-press item-use behavior through its own host implementation.",
    };

    public LongLiveCompatibilityRedirectDescriptor Redirect => new LongLiveCompatibilityRedirectDescriptor
    {
        RedirectId = LongLiveEasyBatchUpdateRedirect.RedirectId,
        SourceLibraryId = "easybatch",
        CapabilityFamily = "bulk-item-use",
        TargetSurface = typeof(LongLiveEasyBatchUpdateRedirect).FullName ?? nameof(LongLiveEasyBatchUpdateRedirect),
        DetectionTypeName = "EasyBatch.Plugin",
        DetectionMethodName = "Update",
        EnabledByDefault = true,
        Notes = "LongLive owns the long-press batch-use path when EasyBatch is present.",
    };

    public LongLiveCompatibilityActivationRecord Install()
    {
        var sourceDetected = _compatibilityRuntime.IsTypeAvailable(Library.DetectionTypeName);
        var updateMethod = _compatibilityRuntime.ResolveMethod(Library.DetectionTypeName, Redirect.DetectionMethodName);
        var redirectResult = _redirect.Install();
        var redirectEnabled = LongLiveCompatibilityOptionGate.IsEasyBatchEnabled();
        var redirectApplied = redirectEnabled && sourceDetected && updateMethod is not null && redirectResult.Applied;

        return LongLiveCompatibilityActivationFactory.Create(
            Redirect.RedirectId,
            Redirect.SourceLibraryId,
            sourceDetected,
            redirectEnabled,
            redirectApplied,
            sourceDetected
                ? (updateMethod is null
                    ? "missing-method"
                    : (redirectApplied ? "redirect-installed" : redirectResult.StatusCode))
                : "source-missing",
            sourceDetected
                ? (updateMethod is null
                    ? LongLiveCompatibilityText.Get("compatibility.easybatch.missing_method", "EasyBatch.Plugin detected, but Update was not resolved.")
                    : (redirectApplied
                        ? (string.IsNullOrWhiteSpace(redirectResult.Detail)
                            ? LongLiveCompatibilityText.Get("compatibility.easybatch.redirect_installed", "EasyBatch.Update redirect is installed and owned by LongLive.")
                            : LongLiveCompatibilityText.Get("compatibility.easybatch.redirect_installed", "EasyBatch.Update redirect is installed and owned by LongLive.") + " " + redirectResult.Detail)
                        : (string.IsNullOrWhiteSpace(redirectResult.Detail)
                            ? LongLiveCompatibilityText.Get("compatibility.easybatch.redirect_pending", "EasyBatch.Plugin.Update is available, but the LongLive redirect did not finish installing.")
                            : redirectResult.Detail)))
                : LongLiveCompatibilityText.Get("compatibility.easybatch.source_missing", "EasyBatch.Plugin was not detected in the current host runtime."));
    }
}
