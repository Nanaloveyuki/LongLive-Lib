using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

[BepInPlugin(LongLivePluginMetadata.PluginGuid, LongLivePluginMetadata.PluginName, LongLivePluginMetadata.PluginVersion)]
public sealed class LongLivePlugin : BaseUnityPlugin
{
    private Harmony? _harmony;
    private NextRuntimeFacade? _runtime;
    private LongLiveBootstrapper? _bootstrapper;
    private LongLiveHostOptions? _options;

    internal static LongLivePlugin? Instance { get; private set; }

    internal static ManualLogSource? LogSource { get; private set; }

    public NextRuntimeFacade Runtime => _runtime ??= NextRuntimeFactory.Create();

    public LongLiveHostOptions Options => _options ??= CreateOptions();

    private void Awake()
    {
        Instance = this;
        LogSource = Logger;

        Logger.LogInfo($"{LongLivePluginMetadata.PluginName} plugin awake.");
        _harmony = new Harmony(LongLivePluginMetadata.PluginGuid);
        _harmony.PatchAll(typeof(LongLivePlugin).Assembly);
        _bootstrapper = new LongLiveBootstrapper(Logger, Runtime, Options);
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
            enableDemoCommandRegistration,
            enableDemoQueryRegistration,
            enableJsonModDemoInstall,
            jsonModDemoPath,
            contentBackend);
    }
}
