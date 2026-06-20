using BepInEx.Configuration;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveHostOptions
{
    public LongLiveHostOptions(
        ConfigEntry<bool> enableDebugLogging,
        ConfigEntry<bool> enableContentRuntimeInspection,
        ConfigEntry<bool> enableNativeProbe,
        ConfigEntry<string> nativeLibraryPath,
        ConfigEntry<bool> enableMapTrace,
        ConfigEntry<bool> enableMapTraceVerbose,
        ConfigEntry<bool> enableAutoExportMapSnapshot,
        ConfigEntry<bool> enableBattleTrace,
        ConfigEntry<bool> enableBattleTraceVerbose,
        ConfigEntry<bool> enableExperimentalBattleGuard,
        ConfigEntry<bool> enableBulkItemUseOptimization,
        ConfigEntry<int> bulkItemUseChunkSize,
        ConfigEntry<float> bulkItemUseFrameBudgetMs,
        ConfigEntry<bool> enableEasyBatchCompatibility,
        ConfigEntry<bool> enableWhiteZeCompatibility,
        ConfigEntry<bool> enableVToolsCompatibility,
        ConfigEntry<bool> enableDemoCommandRegistration,
        ConfigEntry<bool> enableDemoQueryRegistration,
        ConfigEntry<bool> enableJsonModDemoInstall,
        ConfigEntry<string> jsonModDemoPath,
        ConfigEntry<string> contentBackend)
    {
        EnableDebugLogging = enableDebugLogging;
        EnableContentRuntimeInspection = enableContentRuntimeInspection;
        EnableNativeProbe = enableNativeProbe;
        NativeLibraryPath = nativeLibraryPath;
        EnableMapTrace = enableMapTrace;
        EnableMapTraceVerbose = enableMapTraceVerbose;
        EnableAutoExportMapSnapshot = enableAutoExportMapSnapshot;
        EnableBattleTrace = enableBattleTrace;
        EnableBattleTraceVerbose = enableBattleTraceVerbose;
        EnableExperimentalBattleGuard = enableExperimentalBattleGuard;
        EnableBulkItemUseOptimization = enableBulkItemUseOptimization;
        BulkItemUseChunkSize = bulkItemUseChunkSize;
        BulkItemUseFrameBudgetMs = bulkItemUseFrameBudgetMs;
        EnableEasyBatchCompatibility = enableEasyBatchCompatibility;
        EnableWhiteZeCompatibility = enableWhiteZeCompatibility;
        EnableVToolsCompatibility = enableVToolsCompatibility;
        EnableDemoCommandRegistration = enableDemoCommandRegistration;
        EnableDemoQueryRegistration = enableDemoQueryRegistration;
        EnableJsonModDemoInstall = enableJsonModDemoInstall;
        JsonModDemoPath = jsonModDemoPath;
        ContentBackend = contentBackend;
    }

    public ConfigEntry<bool> EnableDebugLogging { get; }

    public ConfigEntry<bool> EnableContentRuntimeInspection { get; }

    public ConfigEntry<bool> EnableNativeProbe { get; }

    public ConfigEntry<string> NativeLibraryPath { get; }

    public ConfigEntry<bool> EnableMapTrace { get; }

    public ConfigEntry<bool> EnableMapTraceVerbose { get; }

    public ConfigEntry<bool> EnableAutoExportMapSnapshot { get; }

    public ConfigEntry<bool> EnableBattleTrace { get; }

    public ConfigEntry<bool> EnableBattleTraceVerbose { get; }

    public ConfigEntry<bool> EnableExperimentalBattleGuard { get; }

    public ConfigEntry<bool> EnableBulkItemUseOptimization { get; }

    public ConfigEntry<int> BulkItemUseChunkSize { get; }

    public ConfigEntry<float> BulkItemUseFrameBudgetMs { get; }

    public ConfigEntry<bool> EnableEasyBatchCompatibility { get; }

    public ConfigEntry<bool> EnableWhiteZeCompatibility { get; }

    public ConfigEntry<bool> EnableVToolsCompatibility { get; }

    public ConfigEntry<bool> EnableDemoCommandRegistration { get; }

    public ConfigEntry<bool> EnableDemoQueryRegistration { get; }

    public ConfigEntry<bool> EnableJsonModDemoInstall { get; }

    public ConfigEntry<string> JsonModDemoPath { get; }

    public ConfigEntry<string> ContentBackend { get; }
}
