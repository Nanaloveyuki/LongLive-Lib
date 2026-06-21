using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using LongLive.BepInEx.Native;
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
        var enableDebugLogging = Config.Bind(
            "LongLive",
            "EnableDebugLogging",
            false,
            "Enable additional LongLive host bootstrap logging.");

        var enableContentRuntimeInspection = Config.Bind(
            "LongLive",
            "EnableContentRuntimeInspection",
            false,
            "Log a read-only inspection report of Next content runtime entry points during bootstrap.");

        var enableNativeProbe = Config.Bind(
            "LongLive",
            "EnableNativeProbe",
            false,
            "Attempt a native Rust-core DllImport probe during host bootstrap.");

        var nativeLibraryPath = Config.Bind(
            "LongLive",
            "NativeLibraryPath",
            string.Empty,
            "Optional explicit path to longlive_native_core.dll used by the native probe installer.");

        var enableMapTrace = Config.Bind(
            "LongLive",
            "EnableMapTrace",
            false,
            "Enable read-only Harmony map tracing for scene loads, world-map runtime state, node registration, and overview-map UI snapshots. Effective only when EnableDebugLogging is also true.");

        var enableMapTraceVerbose = Config.Bind(
            "LongLive",
            "EnableMapTraceVerbose",
            false,
            "Enable additional map trace detail such as sampled node inventories and repeated structure snapshots. Effective only when both EnableDebugLogging and EnableMapTrace are true.");

        var enableMapOverviewRuntimeLogging = Config.Bind(
            "LongLive",
            "EnableMapOverviewRuntimeLogging",
            false,
            "Enable LongLive map-overview runtime summary logs for registered pages, regions, and route projections. Effective only when EnableDebugLogging is also true.");

        var enableMapOverviewRuntimeVerbose = Config.Bind(
            "LongLive",
            "EnableMapOverviewRuntimeVerbose",
            false,
            "Enable additional map-overview runtime detail such as per-page and per-mod registration samples. Effective only when both EnableDebugLogging and EnableMapOverviewRuntimeLogging are true.");

        var enableCustomMapRuntimeLogging = Config.Bind(
            "LongLive",
            "EnableCustomMapRuntimeLogging",
            false,
            "Enable LongLive custom-map runtime planning logs for registered runtime scenes, bootstraps, and active-scene matches. Effective only when EnableDebugLogging is also true.");

        var enableCustomMapRuntimeVerbose = Config.Bind(
            "LongLive",
            "EnableCustomMapRuntimeVerbose",
            false,
            "Enable additional custom-map runtime detail such as bootstrap route summaries and per-mod runtime registration samples. Effective only when both EnableDebugLogging and EnableCustomMapRuntimeLogging are true.");

        var enableAutoExportSceneRoutingPlanningDump = Config.Bind(
            "LongLive",
            "EnableAutoExportSceneRoutingPlanningDump",
            false,
            "Export the current scene-routing planning bundle as JSON during runtime installer execution. Effective only when EnableDebugLogging is also true.");

        var enableSceneLocalTopologyLogging = Config.Bind(
            "LongLive",
            "EnableSceneLocalTopologyLogging",
            false,
            "Enable LongLive scene-local-topology binding logs for imported runtime node graphs such as JTools MapInfo. Effective only when EnableDebugLogging is also true.");

        var enableSceneLocalTopologyVerbose = Config.Bind(
            "LongLive",
            "EnableSceneLocalTopologyVerbose",
            false,
            "Enable additional scene-local-topology detail such as sampled node names for the active imported topology. Effective only when both EnableDebugLogging and EnableSceneLocalTopologyLogging are true.");

        var enableAutoExportMapSnapshot = Config.Bind(
            "LongLive",
            "EnableAutoExportMapSnapshot",
            false,
            "Export the current host map snapshot as JSON during runtime installer execution. Effective only when EnableDebugLogging is also true.");

        var enableBattleTrace = Config.Bind(
            "LongLive",
            "EnableBattleTrace",
            false,
            "Enable read-only Harmony battle tracing for fight entry, round flow, and skill usage. Effective only when EnableDebugLogging is also true.");

        var enableBattleTraceVerbose = Config.Bind(
            "LongLive",
            "EnableBattleTraceVerbose",
            false,
            "Enable additional battle trace detail such as runtime field inventories and method snapshots. Effective only when both EnableDebugLogging and EnableBattleTrace are true.");

        var enableExperimentalBattleGuard = Config.Bind(
            "LongLive",
            "EnableExperimentalBattleGuard",
            false,
            "Enable an experimental non-player post-death battle guard that skips further Buff/Spell re-entry once a target is already dead or at HP <= 0.");

        var enableBulkItemUseOptimization = Config.Bind(
            "LongLive",
            "EnableBulkItemUseOptimization",
            true,
            "Enable LongLive bulk-item-use smoothing and pop-tip cleanup for large consumable batches.");

        var bulkItemUseChunkSize = Config.Bind(
            "LongLive",
            "BulkItemUseChunkSize",
            24,
            "Maximum number of item.Use() calls processed per frame when LongLive bulk-item-use smoothing is active.");

        var bulkItemUseFrameBudgetMs = Config.Bind(
            "LongLive",
            "BulkItemUseFrameBudgetMs",
            3.0f,
            "Approximate per-frame time budget in milliseconds for LongLive bulk-item-use processing.");

        var enablePopTipOptimization = Config.Bind(
            "LongLive",
            "EnablePopTipOptimization",
            true,
            "Enable generic LongLive pop-tip coalescing, queue cleanup, and faster fade behavior for high-volume prompt bursts.");

        var popTipAggregationWindowMs = Config.Bind(
            "LongLive",
            "PopTipAggregationWindowMs",
            500f,
            "Aggregation window in milliseconds for merging repeated non-critical pop-tips into a single summarized entry.");

        var popTipFastModeThreshold = Config.Bind(
            "LongLive",
            "PopTipFastModeThreshold",
            6,
            "When queued or active pop-tip count reaches this threshold, LongLive switches the pop-tip system into a faster cleanup mode.");

        var enableTuJianPinyinSearch = Config.Bind(
            "LongLive",
            "EnableTuJianPinyinSearch",
            true,
            "Enable LongLive pinyin-aware search fallback for the TuJian search path.");

        var enableFadeOptimization = Config.Bind(
            "LongLive",
            "EnableFadeOptimization",
            true,
            "Enable LongLive fade and transition acceleration for shared black-screen and scene-door animations.");

        var fadeDurationScale = Config.Bind(
            "LongLive",
            "FadeDurationScale",
            0.5f,
            "Global scale multiplier for supported fade and transition durations. Lower values are faster.");

        var mapDoorTransitionSeconds = Config.Bind(
            "LongLive",
            "MapDoorTransitionSeconds",
            0.35f,
            "Override duration in seconds for supported map-door black-screen transitions before scene loads.");

        var enableEasyBatchCompatibility = Config.Bind(
            "LongLive.Compatibility",
            "EnableEasyBatchCompatibility",
            true,
            "Enable LongLive compatibility handling for EasyBatch item-use interception.");

        var enableWhiteZeCompatibility = Config.Bind(
            "LongLive.Compatibility",
            "EnableWhiteZeCompatibility",
            true,
            "Enable LongLive compatibility aliases for WhiteZe Tools routing and lightweight queries.");

        var enableVToolsCompatibility = Config.Bind(
            "LongLive.Compatibility",
            "EnableVToolsCompatibility",
            true,
            "Enable LongLive compatibility aliases for VTools routing and lightweight queries.");

        var enableDemoCommandRegistration = Config.Bind(
            "LongLive",
            "EnableDemoCommandRegistration",
            true,
            "Register the demo LongLiveEcho dialog command during bootstrap.");

        var enableDemoQueryRegistration = Config.Bind(
            "LongLive",
            "EnableDemoQueryRegistration",
            true,
            "Register the demo LongLiveDebugEnabled expression query during bootstrap.");

        var enableDemoMapRegistration = Config.Bind(
            "LongLive",
            "EnableDemoMapRegistration",
            true,
            "Register the built-in LongLive demo map draft, topology batch, and route-resolution demo hooks during bootstrap.");

        var enableJsonModDemoInstall = Config.Bind(
            "LongLive",
            "EnableJsonModDemoInstall",
            false,
            "Load, validate, and install the configured JSON mod demo package during bootstrap.");

        var jsonModDemoPath = Config.Bind(
            "LongLive",
            "JsonModDemoPath",
            string.Empty,
            "Optional directory path of the JSON mod demo package used by the bootstrap demo installer.");

        var contentBackend = Config.Bind(
            "LongLive",
            "ContentBackend",
            LongLiveContentBackendKind.Deferred.ToString(),
            "Content backend selection for JSON-mod content entries. Supported values: Deferred, Next.");

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
