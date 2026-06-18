using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapTraceInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveMapTraceInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveMapTraceInstaller";

    public void Install()
    {
        if (!_options.EnableMapTrace.Value)
        {
            return;
        }

        if (!_options.EnableDebugLogging.Value)
        {
            _logger.LogInfo("LongLive map trace is configured, but debug logging is disabled. Skipping map trace activation.");
            return;
        }

        _logger.LogInfo($"LongLive map trace installer active. verbose={_options.EnableMapTraceVerbose.Value}");
        _logger.LogInfo("Map trace records read-only scene, world-map, node, and overview-map snapshots.");
    }
}
