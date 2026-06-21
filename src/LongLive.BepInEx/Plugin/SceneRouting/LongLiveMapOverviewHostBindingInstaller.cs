using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewHostBindingInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveMapOverviewHostBindingInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveMapOverviewHostBindingInstaller";

    public void Install()
    {
        LongLiveMapOverviewHostBindingRuntime.Initialize(_logger, _options);
        LongLiveMapOverviewHostBindingRuntime.LogInstallerSummary();
    }
}
