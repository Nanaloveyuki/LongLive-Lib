using System;
using BepInEx.Logging;
using LongLive.Mods.Installation;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveContentRegistryProvider
{
    private readonly ManualLogSource _logger;
    private readonly NextRuntimeFacade _runtime;
    private readonly LongLiveHostOptions _options;

    public LongLiveContentRegistryProvider(ManualLogSource logger, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ILongLiveContentRegistry CreateRegistry()
    {
        var backendKind = GetBackendKind();
        switch (backendKind)
        {
            case LongLiveContentBackendKind.Next:
                _logger.LogInfo("Using LongLive Next content backend shell.");
                return new LongLiveNextContentRegistry(_logger, _runtime);
            default:
                _logger.LogInfo("Using LongLive deferred content backend.");
                return new LongLiveDeferredContentRegistry();
        }
    }

    public LongLiveContentBackendKind GetBackendKind()
    {
        var rawValue = _options.ContentBackend.Value;
        if (Enum.TryParse<LongLiveContentBackendKind>(rawValue, ignoreCase: true, out var backendKind))
        {
            return backendKind;
        }

        _logger.LogWarning($"Unknown content backend '{rawValue}'. Falling back to Deferred.");
        return LongLiveContentBackendKind.Deferred;
    }
}
