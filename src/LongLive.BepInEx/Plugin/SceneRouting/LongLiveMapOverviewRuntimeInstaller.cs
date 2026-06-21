using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapOverviewRuntimeInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveMapOverviewRuntimeInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveMapOverviewRuntimeInstaller";

    public void Install()
    {
        LongLiveMapOverviewRuntime.Initialize(_logger, _options);
        LongLiveMapOverviewRuntime.LogInstallerSummary();
        if (_options.EnableDebugLogging.Value)
        {
            var report = LongLiveMapOverviewExecutionExecutor.ExecuteDryRun(LongLivePluginContext.GetMapOverviewExecutionPlan(), _logger);
            _logger.LogInfo($"[MapOverviewExecutor] preflight summary: steps={report.StepCount}, success={report.SuccessCount}, failure={report.FailureCount}");
        }
    }
}
