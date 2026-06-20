using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveSceneLocalTopologyInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveSceneLocalTopologyInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveSceneLocalTopologyInstaller";

    public void Install()
    {
        LongLiveSceneLocalTopologyRuntime.Initialize(_logger, _options);
        LongLiveSceneLocalTopologyRuntime.LogInstallerSummary();
    }
}
