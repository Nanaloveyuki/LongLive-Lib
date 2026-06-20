using System;
using LongLive.Mods.Compatibility;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveWhiteZeCompatibilityFeature : ILongLiveCompatibilityFeature
{
    private readonly LongLiveCompatibilityRuntime _compatibilityRuntime;
    private readonly LongLiveWhiteZeRoutingAdapter _adapter;

    public LongLiveWhiteZeCompatibilityFeature(NextRuntimeFacade runtime, LongLiveCompatibilityRuntime compatibilityRuntime)
    {
        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        _compatibilityRuntime = compatibilityRuntime ?? throw new ArgumentNullException(nameof(compatibilityRuntime));
        _adapter = new LongLiveWhiteZeRoutingAdapter(runtime, compatibilityRuntime);
    }

    public LongLiveCompatibilityLibraryDescriptor Library => new LongLiveCompatibilityLibraryDescriptor
    {
        LibraryId = "whiteze-tools",
        DisplayName = "WhiteZe Tools",
        RelationshipMode = LongLiveCompatibilityRelationshipMode.CapabilityCompatible,
        CapabilityFamily = "scene-routing",
        DetectionTypeName = "top.Isteyft.MCS.IsTools.Util.WarpUtils",
        Notes = "Reference utility family for scene routing and warp behavior.",
    };

    public LongLiveCompatibilityRedirectDescriptor Redirect => new LongLiveCompatibilityRedirectDescriptor
    {
        RedirectId = LongLiveWhiteZeRoutingAdapter.RedirectId,
        SourceLibraryId = "whiteze-tools",
        CapabilityFamily = "scene-routing",
        TargetSurface = typeof(LongLiveWhiteZeRoutingAdapter).FullName ?? nameof(LongLiveWhiteZeRoutingAdapter),
        DetectionTypeName = "SkySwordKill.Next.Main",
        DetectionMethodName = "RegisterCommand/RegisterEnvQuery",
        EnabledByDefault = true,
        Notes = "Registers Next-facing routing/query aliases compatible with common WhiteZe patterns.",
    };

    public LongLiveCompatibilityActivationRecord Install()
    {
        var sourceDetected = _compatibilityRuntime.IsTypeAvailable(Library.DetectionTypeName);
        var redirectEnabled = LongLiveCompatibilityOptionGate.IsWhiteZeToolsEnabled();
        if (_adapter.IsAvailable)
        {
            _adapter.Register();
        }

        return LongLiveCompatibilityActivationFactory.Create(
            Redirect.RedirectId,
            Redirect.SourceLibraryId,
            sourceDetected,
            redirectEnabled,
            redirectEnabled && _adapter.IsAvailable,
            _adapter.IsAvailable
                ? (sourceDetected ? "registered-source-present" : "registered-source-missing")
                : "next-unavailable",
            _adapter.IsAvailable
                ? (sourceDetected
                    ? LongLiveCompatibilityText.Get("compatibility.whiteze.registered_present", "WhiteZe-style Next routing/query adapters were registered, and the reference WhiteZe runtime surface is present.")
                    : LongLiveCompatibilityText.Get("compatibility.whiteze.registered_missing", "WhiteZe-style Next routing/query adapters were registered, but the reference WhiteZe runtime surface was not detected in the current host."))
                : LongLiveCompatibilityText.Get("compatibility.whiteze.next_unavailable", "WhiteZe-style Next routing/query adapters were skipped because Next runtime command/query registration is unavailable."));
    }
}
