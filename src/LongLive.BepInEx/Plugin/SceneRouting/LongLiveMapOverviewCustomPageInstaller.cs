using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewCustomPageInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveMapOverviewCustomPageInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveMapOverviewCustomPageInstaller";

    public void Install()
    {
        LongLiveMapOverviewCustomPageRuntime.Initialize(_logger, _options);
    }
}
