using System;
using LongLive.Mods.Compatibility;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveVToolsCompatibilityFeature : ILongLiveCompatibilityFeature
{
    private readonly LongLiveCompatibilityRuntime _compatibilityRuntime;
    private readonly LongLiveVToolsRoutingAdapter _adapter;

    public LongLiveVToolsCompatibilityFeature(NextRuntimeFacade runtime, LongLiveCompatibilityRuntime compatibilityRuntime)
    {
        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        _compatibilityRuntime = compatibilityRuntime ?? throw new ArgumentNullException(nameof(compatibilityRuntime));
        _adapter = new LongLiveVToolsRoutingAdapter(runtime, compatibilityRuntime);
    }

    public LongLiveCompatibilityLibraryDescriptor Library => new LongLiveCompatibilityLibraryDescriptor
    {
        LibraryId = "vtools",
        DisplayName = "VTools",
        RelationshipMode = LongLiveCompatibilityRelationshipMode.AdapterCompatible,
        CapabilityFamily = "scene-routing",
        DetectionTypeName = "Ventulus.VTools",
        Notes = "Broad utility facade and possible future adapter target for routing/query surfaces.",
    };

    public LongLiveCompatibilityRedirectDescriptor Redirect => new LongLiveCompatibilityRedirectDescriptor
    {
        RedirectId = LongLiveVToolsRoutingAdapter.RedirectId,
        SourceLibraryId = "vtools",
        CapabilityFamily = "scene-routing",
        TargetSurface = typeof(LongLiveVToolsRoutingAdapter).FullName ?? nameof(LongLiveVToolsRoutingAdapter),
        DetectionTypeName = "SkySwordKill.Next.Main",
        DetectionMethodName = "RegisterCommand/RegisterEnvQuery",
        EnabledByDefault = true,
        Notes = "Registers Next-facing routing/query aliases compatible with common VTools patterns.",
    };

    public LongLiveCompatibilityActivationRecord Install()
    {
        var sourceDetected = _compatibilityRuntime.IsTypeAvailable(Library.DetectionTypeName);
        var redirectEnabled = LongLiveCompatibilityOptionGate.IsVToolsEnabled();
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
                    ? LongLiveCompatibilityText.Get("compatibility.vtools.registered_present", "VTools-style Next routing/query adapters were registered, and the reference VTools runtime surface is present.")
                    : LongLiveCompatibilityText.Get("compatibility.vtools.registered_missing", "VTools-style Next routing/query adapters were registered, but the reference VTools runtime surface was not detected in the current host."))
                : LongLiveCompatibilityText.Get("compatibility.vtools.next_unavailable", "VTools-style Next routing/query adapters were skipped because Next runtime command/query registration is unavailable."));
    }
}
