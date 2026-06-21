using System;
using BepInEx.Logging;
using LongLive.Mods.Maps;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapDemoInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly NextRuntimeFacade _runtime;
    private readonly LongLiveHostOptions _options;

    public LongLiveMapDemoInstaller(ManualLogSource logger, NextRuntimeFacade runtime, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveMapDemoInstaller";

    public void Install()
    {
        if (!_options.EnableDemoMapRegistration.Value)
        {
            return;
        }

        var draft = LongLiveMapDemoDraftFactory.CreateDraft();
        var topology = LongLiveMapDemoTopologyFactory.CreateBatch();
        var plan = LongLivePluginContext.RegisterMapRegistryDraft(draft, sourceName: "demo.map");
        var demoRuntime = new LongLiveMapDemoRuntimeService(_logger, _runtime);
        LongLivePluginContext.RegisterSceneLocalTopologyBatch(topology);

        demoRuntime.PublishRegistrationState(plan, topology);
        RegisterRuntimeHooks(demoRuntime);

        _logger.LogInfo($"LongLive map demo registered. pages={LongLiveMapDemoConstants.PageId},{LongLiveMapDemoConstants.SecondPageId}, scenes={LongLiveMapDemoConstants.CustomSceneId},{LongLiveMapDemoConstants.SecondCustomSceneId}, topologies={LongLiveMapDemoConstants.TopologyId},{LongLiveMapDemoConstants.SecondTopologyId}");
    }

    private void RegisterRuntimeHooks(LongLiveMapDemoRuntimeService demoRuntime)
    {
        if (_runtime.CommandRegistry.IsAvailable)
        {
            try
            {
                _runtime.CommandRegistry.Register(
                    "LongLiveDemoMapDump",
                    (_, complete) =>
                    {
                        demoRuntime.DumpAllocationState();
                        complete();
                    });

                _runtime.CommandRegistry.Register(
                    "LongLiveDemoMapResolve",
                    (_, complete) =>
                    {
                        demoRuntime.ResolveDemoRoute();
                        complete();
                    });

                _runtime.CommandRegistry.Register(
                    "LongLiveDemoMapExportPlanningDump",
                    (_, complete) =>
                    {
                        demoRuntime.ExportPlanningDump();
                        complete();
                    });

                _logger.LogInfo("Registered demo map commands: LongLiveDemoMapDump, LongLiveDemoMapResolve, LongLiveDemoMapExportPlanningDump");
            }
            catch (PlatformNotSupportedException exception)
            {
                _logger.LogWarning($"Skipping demo map command registration: {exception.Message}");
            }
        }

        if (_runtime.QueryRegistry.IsAvailable)
        {
            try
            {
                _runtime.QueryRegistry.Register(
                    "LongLiveDemoMapRegistered",
                    _ => _runtime.GetInt(LongLiveMapDemoStateKeys.Registered, 0));

                _runtime.QueryRegistry.Register(
                    "LongLiveDemoMapRouteKind",
                    _ => _runtime.GetString(LongLiveMapDemoStateKeys.RouteKind, string.Empty));

                _runtime.QueryRegistry.Register(
                    "LongLiveDemoMapPlanSummary",
                    _ => _runtime.GetString(LongLiveMapDemoStateKeys.PlanSummary, string.Empty));

                _runtime.QueryRegistry.Register(
                    "LongLiveDemoMapResolveStatus",
                    _ => _runtime.GetString(LongLiveMapDemoStateKeys.ResolveStatus, string.Empty));

                _logger.LogInfo("Registered demo map queries: LongLiveDemoMapRegistered, LongLiveDemoMapRouteKind, LongLiveDemoMapPlanSummary, LongLiveDemoMapResolveStatus");
            }
            catch (PlatformNotSupportedException exception)
            {
                _logger.LogWarning($"Skipping demo map query registration: {exception.Message}");
            }
        }
    }
}
