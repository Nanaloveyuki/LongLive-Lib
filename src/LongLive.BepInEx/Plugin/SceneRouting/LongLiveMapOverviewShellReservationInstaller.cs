using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewShellReservationInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveMapOverviewShellReservationInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveMapOverviewShellReservationInstaller";

    public void Install()
    {
        LongLiveMapOverviewShellReservationRuntime.Initialize(_logger, _options);
        LongLiveMapOverviewShellReservationRuntime.LogInstallerSummary();
    }
}
