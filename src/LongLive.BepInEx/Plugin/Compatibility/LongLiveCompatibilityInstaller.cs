using System;
using System.Collections.Generic;
using BepInEx.Logging;
using LongLive.Mods.Compatibility;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCompatibilityInstaller : ILongLiveInstaller
{
    private readonly LongLiveCompatibilityRuntime _runtime;
    private readonly NextRuntimeFacade _nextRuntime;
    private readonly ManualLogSource _logger;

    public LongLiveCompatibilityInstaller(ManualLogSource logger, LongLiveCompatibilityRuntime runtime, NextRuntimeFacade nextRuntime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _nextRuntime = nextRuntime ?? throw new ArgumentNullException(nameof(nextRuntime));
    }

    public string Name => "LongLiveCompatibilityInstaller";

    public void Install()
    {
        RegisterStaticLibraryDescriptors();
        RegisterStaticRedirectDescriptors();
        RegisterCompatibilityFeatures();
    }

    private void RegisterCompatibilityFeatures()
    {
        foreach (var feature in CreateFeatures())
        {
            _runtime.Registry.RegisterLibrary(feature.Library);
            _runtime.Registry.RegisterRedirect(feature.Redirect);

            try
            {
                _runtime.RecordActivation(feature.Install());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Compatibility feature install failed: redirect={feature.Redirect.RedirectId}, library={feature.Library.LibraryId}, error={ex.GetType().Name}: {ex.Message}");
                _logger.LogError(ex);

                _runtime.RecordActivation(LongLiveCompatibilityActivationFactory.Create(
                    feature.Redirect.RedirectId,
                    feature.Redirect.SourceLibraryId,
                    _runtime.IsTypeAvailable(feature.Library.DetectionTypeName),
                    false,
                    false,
                    "install-error",
                    $"{ex.GetType().Name}: {ex.Message}"));
            }
        }
    }

    private IEnumerable<ILongLiveCompatibilityFeature> CreateFeatures()
    {
        yield return new LongLiveEasyBatchCompatibilityFeature(_logger, _runtime);
        yield return new LongLiveWhiteZeCompatibilityFeature(_nextRuntime, _runtime);
        yield return new LongLiveVToolsCompatibilityFeature(_nextRuntime, _runtime);
    }

    private void RegisterStaticLibraryDescriptors()
    {
        _runtime.Registry.RegisterLibrary(new LongLiveCompatibilityLibraryDescriptor
        {
            LibraryId = "bepinex",
            DisplayName = "BepInEx",
            RelationshipMode = LongLiveCompatibilityRelationshipMode.FoundationDependency,
            CapabilityFamily = "host-runtime",
            DetectionTypeName = "BepInEx.BaseUnityPlugin",
            Notes = "Primary host runtime foundation.",
        });

        _runtime.Registry.RegisterLibrary(new LongLiveCompatibilityLibraryDescriptor
        {
            LibraryId = "next",
            DisplayName = "Next",
            RelationshipMode = LongLiveCompatibilityRelationshipMode.BridgeDependency,
            CapabilityFamily = "content-bridge",
            DetectionTypeName = "SkySwordKill.Next.Main",
            Notes = "Bridge dependency for content-side integration.",
        });

    }

    private void RegisterStaticRedirectDescriptors()
    {
    }

}
