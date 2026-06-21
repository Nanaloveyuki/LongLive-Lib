using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveCustomMapRuntimeInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveCustomMapRuntimeInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveCustomMapRuntimeInstaller";

    public void Install()
    {
        LongLiveCustomMapRuntimeState.Initialize(_logger, _options);
        LongLiveCustomMapRuntimeActivationRuntime.Initialize(_logger, _options);
        LongLiveCustomMapRuntimeActivationArtifactRegistry.Initialize(_logger);
        LongLiveCustomMapRuntimeState.LogInstallerSummary();
        LongLiveCustomMapRuntimeActivationRuntime.LogInstallerSummary();
        if (_options.EnableDebugLogging.Value)
        {
            var activationReport = LongLiveCustomMapRuntimeActivationExecutor.ExecutePlan(LongLivePluginContext.GetCustomMapRuntimeActivationExecutionPlan(), _logger, logSteps: true);
            LongLiveCustomMapRuntimeActivationArtifactRegistry.ApplyReport(activationReport, LongLivePluginContext.GetCustomMapRuntimeActivationRuntimeSnapshot(int.MaxValue));
            _logger.LogInfo($"[CustomMapRuntimeActivationExecutor] summary: steps={activationReport.StepCount}, success={activationReport.SuccessCount}, failure={activationReport.FailureCount}");
            var report = LongLiveCustomMapRuntimeExecutionExecutor.ExecuteDryRun(LongLivePluginContext.GetCustomMapRuntimeExecutionPlan(), _logger);
            _logger.LogInfo($"[CustomMapRuntimeExecutor] preflight summary: steps={report.StepCount}, success={report.SuccessCount}, failure={report.FailureCount}");
        }
    }
}
