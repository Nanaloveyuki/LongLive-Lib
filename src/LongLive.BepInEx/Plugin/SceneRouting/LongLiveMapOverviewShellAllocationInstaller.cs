using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewShellAllocationInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveMapOverviewShellAllocationInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveMapOverviewShellAllocationInstaller";

    public void Install()
    {
        LongLiveMapOverviewShellAllocationRuntime.Initialize(_logger, _options);
        LongLiveMapOverviewShellAllocationRuntime.LogInstallerSummary();
    }
}
