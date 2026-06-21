using System;
using BepInEx.Logging;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveSceneRoutingInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;

    public LongLiveSceneRoutingInstaller(ManualLogSource logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "LongLiveSceneRoutingInstaller";

    public void Install()
    {
        var service = LongLivePluginContext.SceneRouting;
        var host = LongLivePluginContext.SceneRoutingHost;
        RegisterHostSnapshot(host.Coordinator);
        host.InitializeFeatures();
        var snapshot = service.CaptureSnapshot();
        var registration = host.CaptureRegistrationSnapshot();
        _logger.LogInfo($"LongLive scene routing ready. activeScene={snapshot.ActiveSceneName}, kind={snapshot.ActiveSceneKind}, place={snapshot.PlaceName}, registeredRoutes={registration.RouteCount}, pages={registration.WorldMapPageCount}, regions={registration.HighlightRegionCount}, nodes={registration.WorldNodeCount}, runtimeScenes={registration.CustomRuntimeSceneCount}, topologies={registration.SceneLocalTopologyCount}");
        LongLiveSceneRoutingStartupReporter.LogSummary(_logger);
    }

    private void RegisterHostSnapshot(LongLiveSceneRoutingCoordinator coordinator)
    {
        try
        {
            var source = new LongLiveHostMapSnapshotRouteSource();
            var plan = source.CreatePlan();

            if (!plan.Validation.IsValid)
            {
                foreach (var issue in plan.Validation.Issues)
                {
                    if (issue.IsError)
                    {
                        _logger.LogWarning($"LongLive scene routing skipped host snapshot issue: {issue.Code}: {issue.Message}");
                    }
                }

                return;
            }

            coordinator.RegisterPlan(plan, source.Name);
            _logger.LogInfo($"LongLive scene routing registered host snapshot routes: scenes={plan.Draft.Scenes.Count}, source={source.Name}");
        }
        catch (Exception exception)
        {
            _logger.LogWarning($"LongLive scene routing could not register host snapshot routes: {exception.GetType().Name}: {exception.Message}");
        }
    }
}
