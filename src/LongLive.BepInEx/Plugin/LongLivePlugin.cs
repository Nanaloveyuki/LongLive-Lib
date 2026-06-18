using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
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

    internal static LongLivePlugin? Instance { get; private set; }

    internal static ManualLogSource? LogSource { get; private set; }

    public NextRuntimeFacade Runtime => _runtime ??= NextRuntimeFactory.Create();

    public LongLiveNativeService Native => _native ??= new LongLiveNativeService(Logger);

    public LongLiveHostOptions Options => _options ??= CreateOptions();

    private void Awake()
    {
        Instance = this;
        LogSource = Logger;

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

        _bootstrapper = new LongLiveBootstrapper(Logger, Runtime, Native, Options);
        _bootstrapper.Initialize();
    }

    private void OnEnable()
    {
        Logger.LogDebug("LongLive plugin enabled.");
    }

    private void OnDestroy()
    {
        _bootstrapper?.Shutdown();
        _harmony?.UnpatchSelf();
        Logger.LogInfo("LongLive plugin destroyed.");
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
            enableBattleTrace,
            enableBattleTraceVerbose,
            enableExperimentalBattleGuard,
            enableBulkItemUseOptimization,
            bulkItemUseChunkSize,
            bulkItemUseFrameBudgetMs,
            enableDemoCommandRegistration,
            enableDemoQueryRegistration,
            enableJsonModDemoInstall,
            jsonModDemoPath,
            contentBackend);
    }
}
