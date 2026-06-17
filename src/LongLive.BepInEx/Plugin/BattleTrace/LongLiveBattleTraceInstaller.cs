using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveBattleTraceInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveBattleTraceInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveBattleTraceInstaller";

    public void Install()
    {
        if (!_options.EnableBattleTrace.Value)
        {
            return;
        }

        if (!_options.EnableDebugLogging.Value)
        {
            _logger.LogInfo("LongLive battle trace is configured, but debug logging is disabled. Skipping battle trace activation.");
            return;
        }

        _logger.LogInfo($"LongLive battle trace installer active. verbose={_options.EnableBattleTraceVerbose.Value}");
        _logger.LogInfo("Battle trace records read-only fight entry, round flow, and skill usage snapshots.");
    }
}
