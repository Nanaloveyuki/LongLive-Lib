using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using LongLive.BepInEx.Native;
using LongLive.BepInEx.Plugin.Configuration;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

[BepInPlugin(LongLivePluginMetadata.PluginGuid, LongLivePluginMetadata.PluginName, LongLivePluginMetadata.PluginVersion)]
public sealed class LongLivePlugin : BaseUnityPlugin
{
    private Harmony? _harmony;
    private LongLiveNativeService? _native;
    private NextRuntimeFacade? _runtime;
    private LongLiveBootstrapper? _bootstrapper;
    private LongLiveHostOptions? _options;
    private LongLiveHostHandshake? _handshake;

    internal static LongLivePlugin? Instance { get; private set; }

    internal static ManualLogSource? LogSource { get; private set; }

    public NextRuntimeFacade Runtime => _runtime ??= NextRuntimeFactory.Create();

    public LongLiveNativeService Native => _native ??= new LongLiveNativeService(Logger);

    public LongLiveHostOptions Options => _options ??= CreateOptions();

    public LongLiveHostHandshake Handshake => _handshake ??= LongLiveHostHandshakeFactory.Create(this);

    internal LongLiveHostHandshake RefreshHandshake()
    {
        _handshake = LongLiveHostHandshakeFactory.Create(this);
        return _handshake;
    }

    private void Awake()
    {
        try
        {
            Instance = this;
            LogSource = Logger;
            LongLiveNextRuntimeProxyInstaller.Install();

            Logger.LogInfo($"{LongLivePluginMetadata.PluginName} plugin awake.");
            _harmony = new Harmony(LongLivePluginMetadata.PluginGuid);
            _harmony.PatchAll(typeof(LongLivePlugin).Assembly);

            if (Options.EnableBattleTrace.Value && Options.EnableDebugLogging.Value)
            {
                Logger.LogInfo($"LongLive battle trace enabled. verbose={Options.EnableBattleTraceVerbose.Value}");
            }
            else if (Options.EnableBattleTrace.Value)
            {
                Logger.LogInfo("LongLive battle trace requested, but debug logging is disabled. Battle trace remains inactive.");
            }

            if (Options.EnableExperimentalBattleGuard.Value)
            {
                Logger.LogInfo("LongLive experimental battle guard enabled. Non-player post-death Buff/Spell re-entry short-circuit is active.");
            }

            Logger.LogInfo(
                "LongLive feature state: " +
                $"bulkItemUse={Options.EnableBulkItemUseOptimization.Value}, " +
                $"popTips={Options.EnablePopTipOptimization.Value}, " +
                $"fade={Options.EnableFadeOptimization.Value}, " +
                $"tuJianPinyin={Options.EnableTuJianPinyinSearch.Value}, " +
                $"debug={Options.EnableDebugLogging.Value}");

            Logger.LogInfo($"LongLive host module MVID: {Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId:D}");

            _bootstrapper = new LongLiveBootstrapper(Logger, Runtime, Native, Options);
            _bootstrapper.Initialize();

            if (Options.EnableDebugLogging.Value)
            {
                var handshake = RefreshHandshake();
                Logger.LogInfo($"LongLive handshake ready. version={handshake.PluginVersion}, protocol={handshake.HandshakeVersion}, next={handshake.NextRuntimeAvailable}, installRoot={handshake.InstallRoot}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"LongLive plugin startup failed: {ex.GetType().Name}: {ex.Message}");
            Logger.LogError(ex);
            throw;
        }
    }

    private void OnEnable()
    {
        Logger.LogDebug("LongLive plugin enabled.");
    }

    private void OnDestroy()
    {
        _bootstrapper?.Shutdown();
        _harmony?.UnpatchSelf();
        LongLiveNativeBridge.ClearCache();
        Logger.LogInfo("LongLive plugin destroyed.");
        _handshake = null;
        LogSource = null;
        Instance = null;
    }

    private LongLiveHostOptions CreateOptions()
    {
        var binder = new LongLiveLocalizedConfigBinder(Config, Runtime);

        var enableDebugLogging = binder.Bind(
            "LongLive",
            "EnableDebugLogging",
            false,
            "config.category.diagnostics",
            "config.enable_debug_logging.name",
            "config.enable_debug_logging.desc",
            300);

        var enableContentRuntimeInspection = binder.Bind(
            "LongLive",
            "EnableContentRuntimeInspection",
            false,
            "config.category.diagnostics",
            "config.enable_content_runtime_inspection.name",
            "config.enable_content_runtime_inspection.desc",
            290);

        var enableNativeProbe = binder.Bind(
            "LongLive",
            "EnableNativeProbe",
            false,
            "config.category.native",
            "config.enable_native_probe.name",
            "config.enable_native_probe.desc",
            280);

        var nativeLibraryPath = binder.Bind(
            "LongLive",
            "NativeLibraryPath",
            string.Empty,
            "config.category.native",
            "config.native_library_path.name",
            "config.native_library_path.desc",
            270);

        var enableMapTrace = binder.Bind(
            "LongLive",
            "EnableMapTrace",
            false,
            "config.category.scene_routing",
            "config.enable_map_trace.name",
            "config.enable_map_trace.desc",
            260);

        var enableMapTraceVerbose = binder.Bind(
            "LongLive",
            "EnableMapTraceVerbose",
            false,
            "config.category.scene_routing",
            "config.enable_map_trace_verbose.name",
            "config.enable_map_trace_verbose.desc",
            250);

        var enableMapOverviewRuntimeLogging = binder.Bind(
            "LongLive",
            "EnableMapOverviewRuntimeLogging",
            false,
            "config.category.scene_routing",
            "config.enable_map_overview_runtime_logging.name",
            "config.enable_map_overview_runtime_logging.desc",
            240);

        var enableMapOverviewRuntimeVerbose = binder.Bind(
            "LongLive",
            "EnableMapOverviewRuntimeVerbose",
            false,
            "config.category.scene_routing",
            "config.enable_map_overview_runtime_verbose.name",
            "config.enable_map_overview_runtime_verbose.desc",
            230);

        var enableCustomMapRuntimeLogging = binder.Bind(
            "LongLive",
            "EnableCustomMapRuntimeLogging",
            false,
            "config.category.scene_routing",
            "config.enable_custom_map_runtime_logging.name",
            "config.enable_custom_map_runtime_logging.desc",
            220);

        var enableCustomMapRuntimeVerbose = binder.Bind(
            "LongLive",
            "EnableCustomMapRuntimeVerbose",
            false,
            "config.category.scene_routing",
            "config.enable_custom_map_runtime_verbose.name",
            "config.enable_custom_map_runtime_verbose.desc",
            210);

        var enableAutoExportSceneRoutingPlanningDump = binder.Bind(
            "LongLive",
            "EnableAutoExportSceneRoutingPlanningDump",
            false,
            "config.category.scene_routing",
            "config.enable_auto_export_scene_routing_planning_dump.name",
            "config.enable_auto_export_scene_routing_planning_dump.desc",
            200);

        var enableSceneLocalTopologyLogging = binder.Bind(
            "LongLive",
            "EnableSceneLocalTopologyLogging",
            false,
            "config.category.scene_routing",
            "config.enable_scene_local_topology_logging.name",
            "config.enable_scene_local_topology_logging.desc",
            190);

        var enableSceneLocalTopologyVerbose = binder.Bind(
            "LongLive",
            "EnableSceneLocalTopologyVerbose",
            false,
            "config.category.scene_routing",
            "config.enable_scene_local_topology_verbose.name",
            "config.enable_scene_local_topology_verbose.desc",
            180);

        var enableAutoExportMapSnapshot = binder.Bind(
            "LongLive",
            "EnableAutoExportMapSnapshot",
            false,
            "config.category.scene_routing",
            "config.enable_auto_export_map_snapshot.name",
            "config.enable_auto_export_map_snapshot.desc",
            170);

        var enableBattleTrace = binder.Bind(
            "LongLive",
            "EnableBattleTrace",
            false,
            "config.category.diagnostics",
            "config.enable_battle_trace.name",
            "config.enable_battle_trace.desc",
            160);

        var enableBattleTraceVerbose = binder.Bind(
            "LongLive",
            "EnableBattleTraceVerbose",
            false,
            "config.category.diagnostics",
            "config.enable_battle_trace_verbose.name",
            "config.enable_battle_trace_verbose.desc",
            150);

        var enableExperimentalBattleGuard = binder.Bind(
            "LongLive",
            "EnableExperimentalBattleGuard",
            false,
            "config.category.gameplay",
            "config.enable_experimental_battle_guard.name",
            "config.enable_experimental_battle_guard.desc",
            140);

        var enableBulkItemUseOptimization = binder.Bind(
            "LongLive",
            "EnableBulkItemUseOptimization",
            true,
            "config.category.gameplay",
            "config.enable_bulk_item_use_optimization.name",
            "config.enable_bulk_item_use_optimization.desc",
            130);

        var bulkItemUseChunkSize = binder.Bind(
            "LongLive",
            "BulkItemUseChunkSize",
            24,
            "config.category.gameplay",
            "config.bulk_item_use_chunk_size.name",
            "config.bulk_item_use_chunk_size.desc",
            120);

        var bulkItemUseFrameBudgetMs = binder.Bind(
            "LongLive",
            "BulkItemUseFrameBudgetMs",
            3.0f,
            "config.category.gameplay",
            "config.bulk_item_use_frame_budget_ms.name",
            "config.bulk_item_use_frame_budget_ms.desc",
            110);

        var enablePopTipOptimization = binder.Bind(
            "LongLive",
            "EnablePopTipOptimization",
            true,
            "config.category.gameplay",
            "config.enable_pop_tip_optimization.name",
            "config.enable_pop_tip_optimization.desc",
            100);

        var popTipAggregationWindowMs = binder.Bind(
            "LongLive",
            "PopTipAggregationWindowMs",
            500f,
            "config.category.gameplay",
            "config.pop_tip_aggregation_window_ms.name",
            "config.pop_tip_aggregation_window_ms.desc",
            90);

        var popTipFastModeThreshold = binder.Bind(
            "LongLive",
            "PopTipFastModeThreshold",
            6,
            "config.category.gameplay",
            "config.pop_tip_fast_mode_threshold.name",
            "config.pop_tip_fast_mode_threshold.desc",
            80);

        var enableTuJianPinyinSearch = binder.Bind(
            "LongLive",
            "EnableTuJianPinyinSearch",
            true,
            "config.category.gameplay",
            "config.enable_tujian_pinyin_search.name",
            "config.enable_tujian_pinyin_search.desc",
            70);

        var enableFadeOptimization = binder.Bind(
            "LongLive",
            "EnableFadeOptimization",
            true,
            "config.category.gameplay",
            "config.enable_fade_optimization.name",
            "config.enable_fade_optimization.desc",
            60);

        var fadeDurationScale = binder.Bind(
            "LongLive",
            "FadeDurationScale",
            0.5f,
            "config.category.gameplay",
            "config.fade_duration_scale.name",
            "config.fade_duration_scale.desc",
            50);

        var mapDoorTransitionSeconds = binder.Bind(
            "LongLive",
            "MapDoorTransitionSeconds",
            0.35f,
            "config.category.gameplay",
            "config.map_door_transition_seconds.name",
            "config.map_door_transition_seconds.desc",
            40);

        var enableEasyBatchCompatibility = binder.Bind(
            "LongLive.Compatibility",
            "EnableEasyBatchCompatibility",
            true,
            "config.category.compatibility",
            "config.enable_easybatch_compatibility.name",
            "config.enable_easybatch_compatibility.desc",
            30);

        var enableWhiteZeCompatibility = binder.Bind(
            "LongLive.Compatibility",
            "EnableWhiteZeCompatibility",
            true,
            "config.category.compatibility",
            "config.enable_whiteze_compatibility.name",
            "config.enable_whiteze_compatibility.desc",
            20);

        var enableVToolsCompatibility = binder.Bind(
            "LongLive.Compatibility",
            "EnableVToolsCompatibility",
            true,
            "config.category.compatibility",
            "config.enable_vtools_compatibility.name",
            "config.enable_vtools_compatibility.desc",
            10);

        var enableDemoCommandRegistration = binder.Bind(
            "LongLive",
            "EnableDemoCommandRegistration",
            true,
            "config.category.demo",
            "config.enable_demo_command_registration.name",
            "config.enable_demo_command_registration.desc",
            0);

        var enableDemoQueryRegistration = binder.Bind(
            "LongLive",
            "EnableDemoQueryRegistration",
            true,
            "config.category.demo",
            "config.enable_demo_query_registration.name",
            "config.enable_demo_query_registration.desc",
            -10);

        var enableDemoMapRegistration = binder.Bind(
            "LongLive",
            "EnableDemoMapRegistration",
            true,
            "config.category.demo",
            "config.enable_demo_map_registration.name",
            "config.enable_demo_map_registration.desc",
            -20);

        var enableJsonModDemoInstall = binder.Bind(
            "LongLive",
            "EnableJsonModDemoInstall",
            false,
            "config.category.content",
            "config.enable_json_mod_demo_install.name",
            "config.enable_json_mod_demo_install.desc",
            -30);

        var jsonModDemoPath = binder.Bind(
            "LongLive",
            "JsonModDemoPath",
            string.Empty,
            "config.category.content",
            "config.json_mod_demo_path.name",
            "config.json_mod_demo_path.desc",
            -40);

        var contentBackend = binder.Bind(
            "LongLive",
            "ContentBackend",
            LongLiveContentBackendKind.Deferred.ToString(),
            "config.category.content",
            "config.content_backend.name",
            "config.content_backend.desc",
            -50,
            new AcceptableValueList<string>(LongLiveContentBackendKind.Deferred.ToString(), LongLiveContentBackendKind.Next.ToString()));

        return new LongLiveHostOptions(
            enableDebugLogging,
            enableContentRuntimeInspection,
            enableNativeProbe,
            nativeLibraryPath,
            enableMapTrace,
            enableMapTraceVerbose,
            enableMapOverviewRuntimeLogging,
            enableMapOverviewRuntimeVerbose,
            enableCustomMapRuntimeLogging,
            enableCustomMapRuntimeVerbose,
            enableAutoExportSceneRoutingPlanningDump,
            enableSceneLocalTopologyLogging,
            enableSceneLocalTopologyVerbose,
            enableAutoExportMapSnapshot,
            enableBattleTrace,
            enableBattleTraceVerbose,
            enableExperimentalBattleGuard,
            enableBulkItemUseOptimization,
            bulkItemUseChunkSize,
            bulkItemUseFrameBudgetMs,
            enablePopTipOptimization,
            popTipAggregationWindowMs,
            popTipFastModeThreshold,
            enableTuJianPinyinSearch,
            enableFadeOptimization,
            fadeDurationScale,
            mapDoorTransitionSeconds,
            enableEasyBatchCompatibility,
            enableWhiteZeCompatibility,
            enableVToolsCompatibility,
            enableDemoCommandRegistration,
            enableDemoQueryRegistration,
            enableDemoMapRegistration,
            enableJsonModDemoInstall,
            jsonModDemoPath,
            contentBackend);
    }
}
