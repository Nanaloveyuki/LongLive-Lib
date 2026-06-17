using System;
using BepInEx.Logging;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveContentInspectionInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly NextRuntimeFacade _runtime;
    private readonly LongLiveHostOptions _options;

    public LongLiveContentInspectionInstaller(ManualLogSource logger, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveContentInspectionInstaller";

    public void Install()
    {
        if (!_options.EnableContentRuntimeInspection.Value)
        {
            return;
        }

        var report = _runtime.ContentInspector.Inspect();
        _logger.LogInfo($"Content runtime inspection available: {report.IsAvailable}");
        _logger.LogInfo($"Content runtime capabilities: main={report.Capabilities.HasMainType}, modManager={report.Capabilities.HasModManager}, mainData={report.Capabilities.HasMainDataContainer}, resources={report.Capabilities.HasResourcesManager}, jsonPatch={report.Capabilities.HasJsonDataPatch}, spritePatch={report.Capabilities.HasModResourcesSpritePatch}, texturePatch={report.Capabilities.HasModResourcesTexturePatch}, localModsDir={report.Capabilities.CanResolveLocalModsDirectory}");

        if (!string.IsNullOrWhiteSpace(report.LocalModsDirectory))
        {
            _logger.LogInfo($"Next local mods directory: {report.LocalModsDirectory}");
        }

        foreach (var type in report.Types)
        {
            _logger.LogInfo($"Content runtime type {type.TypeName}: resolved={type.IsResolved}, properties={type.StaticProperties.Count}, methods={type.StaticMethods.Count}");

            if (_options.EnableDebugLogging.Value)
            {
                foreach (var property in type.StaticProperties)
                {
                    _logger.LogInfo($"  property: {property}");
                }

                foreach (var method in type.StaticMethods)
                {
                    _logger.LogInfo($"  method: {method}");
                }
            }
        }

        foreach (var note in report.Notes)
        {
            _logger.LogInfo($"Content runtime note: {note}");
        }
    }
}
