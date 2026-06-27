using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveZeroTimeMoveNpcRefreshInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveZeroTimeMoveNpcRefreshInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveZeroTimeMoveNpcRefreshInstaller";

    public void Install()
    {
        if (!_options.EnableZeroTimeMoveNpcRefresh.Value)
        {
            _logger.LogInfo("LongLive zero-time move NPC refresh patch is disabled.");
            return;
        }

        _logger.LogInfo("LongLive zero-time move NPC refresh patch active.");
    }
}
