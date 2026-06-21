using System;
using System.IO;
using System.Text.Json;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveSceneRoutingPlanningDumpService
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
    };

    public LongLiveSceneRoutingPlanningBundle CaptureBundle()
    {
        var mapOverviewExecutionPlan = LongLivePluginContext.GetMapOverviewExecutionPlan();
        var customRuntimeExecutionPlan = LongLivePluginContext.GetCustomMapRuntimeExecutionPlan();
        var customRuntimeActivationExecutionPlan = LongLivePluginContext.GetCustomMapRuntimeActivationExecutionPlan();
        return new LongLiveSceneRoutingPlanningBundle
        {
            Registration = LongLivePluginContext.GetSceneRoutingRegistrationSnapshot(),
            MapOverviewInstallPlan = LongLivePluginContext.GetMapOverviewInstallPlan(),
            MapOverviewShellAllocationPlan = LongLivePluginContext.GetMapOverviewShellAllocationPlan(),
            CustomMapRuntimeActivationPlan = LongLivePluginContext.GetCustomMapRuntimeActivationPlan(),
            MapOverviewExecutionPlan = mapOverviewExecutionPlan,
            CustomMapRuntimeExecutionPlan = customRuntimeExecutionPlan,
            CustomMapRuntimeReadinessReport = LongLivePluginContext.GetCustomMapRuntimeStateSnapshot(int.MaxValue).Readiness,
            CustomMapRuntimeActivationRuntime = LongLivePluginContext.GetCustomMapRuntimeActivationRuntimeSnapshot(int.MaxValue),
            CustomMapRuntimeActivationArtifacts = LongLivePluginContext.GetCustomMapRuntimeActivationArtifactSnapshot(int.MaxValue),
            CustomMapRuntimeActivationExecutionPlan = customRuntimeActivationExecutionPlan,
            MapOverviewExecutionReport = LongLiveMapOverviewExecutionExecutor.ExecuteDryRun(mapOverviewExecutionPlan, LongLivePluginContext.GetLogger(), logSteps: false),
            MapOverviewHostBindingRuntime = LongLivePluginContext.GetMapOverviewHostBindingRuntimeSnapshot(int.MaxValue),
            MapOverviewShellAllocationRuntime = LongLivePluginContext.GetMapOverviewShellAllocationRuntimeSnapshot(int.MaxValue),
            MapOverviewShellReservationRuntime = LongLivePluginContext.GetMapOverviewShellReservationRuntimeSnapshot(int.MaxValue),
            CustomMapRuntimeExecutionReport = LongLiveCustomMapRuntimeExecutionExecutor.ExecuteDryRun(customRuntimeExecutionPlan, LongLivePluginContext.GetLogger(), logSteps: false),
            CustomMapRuntimeActivationExecutionReport = LongLiveCustomMapRuntimeActivationExecutor.ExecutePlan(customRuntimeActivationExecutionPlan, LongLivePluginContext.GetLogger(), logSteps: false),
        };
    }

    public LongLiveSceneRoutingPlanningDumpResult ExportCurrentBundle(string? outputDirectory = null)
    {
        try
        {
            var bundle = CaptureBundle();
            var targetDirectory = ResolveOutputDirectory(outputDirectory);
            Directory.CreateDirectory(targetDirectory);

            var fileName = "scene-routing-planning-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json";
            var targetPath = Path.Combine(targetDirectory, fileName);
            var json = JsonSerializer.Serialize(bundle, SerializerOptions);
            File.WriteAllText(targetPath, json);
            return new LongLiveSceneRoutingPlanningDumpResult(true, targetPath, "success");
        }
        catch (Exception exception)
        {
            return new LongLiveSceneRoutingPlanningDumpResult(false, string.Empty, exception.Message);
        }
    }

    private static string ResolveOutputDirectory(string? outputDirectory)
    {
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            return outputDirectory!;
        }

        var pluginAssemblyPath = typeof(LongLivePlugin).Assembly.Location;
        var pluginDirectory = Path.GetDirectoryName(pluginAssemblyPath);
        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            pluginDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }

        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            pluginDirectory = ".";
        }

        return Path.Combine(pluginDirectory, "LongLiveExports");
    }
}
