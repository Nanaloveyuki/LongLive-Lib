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
        ConfigEntry<bool> enableMapOverviewRuntimeLogging,
        ConfigEntry<bool> enableMapOverviewRuntimeVerbose,
        ConfigEntry<bool> enableCustomMapRuntimeLogging,
        ConfigEntry<bool> enableCustomMapRuntimeVerbose,
        ConfigEntry<bool> enableAutoExportSceneRoutingPlanningDump,
        ConfigEntry<bool> enableSceneLocalTopologyLogging,
        ConfigEntry<bool> enableSceneLocalTopologyVerbose,
        ConfigEntry<bool> enableAutoExportMapSnapshot,
        ConfigEntry<bool> enableBattleTrace,
        ConfigEntry<bool> enableBattleTraceVerbose,
        ConfigEntry<bool> enableExperimentalBattleGuard,
        ConfigEntry<bool> enableBulkItemUseOptimization,
        ConfigEntry<int> bulkItemUseChunkSize,
        ConfigEntry<float> bulkItemUseFrameBudgetMs,
        ConfigEntry<bool> enablePopTipOptimization,
        ConfigEntry<float> popTipAggregationWindowMs,
        ConfigEntry<int> popTipFastModeThreshold,
        ConfigEntry<bool> enableTuJianPinyinSearch,
        ConfigEntry<bool> enableFadeOptimization,
        ConfigEntry<float> fadeDurationScale,
        ConfigEntry<float> mapDoorTransitionSeconds,
        ConfigEntry<bool> enableEasyBatchCompatibility,
        ConfigEntry<bool> enableWhiteZeCompatibility,
        ConfigEntry<bool> enableVToolsCompatibility,
        ConfigEntry<bool> enableDemoCommandRegistration,
        ConfigEntry<bool> enableDemoQueryRegistration,
        ConfigEntry<bool> enableDemoMapRegistration,
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
        EnableMapOverviewRuntimeLogging = enableMapOverviewRuntimeLogging;
        EnableMapOverviewRuntimeVerbose = enableMapOverviewRuntimeVerbose;
        EnableCustomMapRuntimeLogging = enableCustomMapRuntimeLogging;
        EnableCustomMapRuntimeVerbose = enableCustomMapRuntimeVerbose;
        EnableAutoExportSceneRoutingPlanningDump = enableAutoExportSceneRoutingPlanningDump;
        EnableSceneLocalTopologyLogging = enableSceneLocalTopologyLogging;
        EnableSceneLocalTopologyVerbose = enableSceneLocalTopologyVerbose;
        EnableAutoExportMapSnapshot = enableAutoExportMapSnapshot;
        EnableBattleTrace = enableBattleTrace;
        EnableBattleTraceVerbose = enableBattleTraceVerbose;
        EnableExperimentalBattleGuard = enableExperimentalBattleGuard;
        EnableBulkItemUseOptimization = enableBulkItemUseOptimization;
        BulkItemUseChunkSize = bulkItemUseChunkSize;
        BulkItemUseFrameBudgetMs = bulkItemUseFrameBudgetMs;
        EnablePopTipOptimization = enablePopTipOptimization;
        PopTipAggregationWindowMs = popTipAggregationWindowMs;
        PopTipFastModeThreshold = popTipFastModeThreshold;
        EnableTuJianPinyinSearch = enableTuJianPinyinSearch;
        EnableFadeOptimization = enableFadeOptimization;
        FadeDurationScale = fadeDurationScale;
        MapDoorTransitionSeconds = mapDoorTransitionSeconds;
        EnableEasyBatchCompatibility = enableEasyBatchCompatibility;
        EnableWhiteZeCompatibility = enableWhiteZeCompatibility;
        EnableVToolsCompatibility = enableVToolsCompatibility;
        EnableDemoCommandRegistration = enableDemoCommandRegistration;
        EnableDemoQueryRegistration = enableDemoQueryRegistration;
        EnableDemoMapRegistration = enableDemoMapRegistration;
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

    public ConfigEntry<bool> EnableMapOverviewRuntimeLogging { get; }

    public ConfigEntry<bool> EnableMapOverviewRuntimeVerbose { get; }

    public ConfigEntry<bool> EnableCustomMapRuntimeLogging { get; }

    public ConfigEntry<bool> EnableCustomMapRuntimeVerbose { get; }

    public ConfigEntry<bool> EnableAutoExportSceneRoutingPlanningDump { get; }

    public ConfigEntry<bool> EnableSceneLocalTopologyLogging { get; }

    public ConfigEntry<bool> EnableSceneLocalTopologyVerbose { get; }

    public ConfigEntry<bool> EnableAutoExportMapSnapshot { get; }

    public ConfigEntry<bool> EnableBattleTrace { get; }

    public ConfigEntry<bool> EnableBattleTraceVerbose { get; }

    public ConfigEntry<bool> EnableExperimentalBattleGuard { get; }

    public ConfigEntry<bool> EnableBulkItemUseOptimization { get; }

    public ConfigEntry<int> BulkItemUseChunkSize { get; }

    public ConfigEntry<float> BulkItemUseFrameBudgetMs { get; }

    public ConfigEntry<bool> EnablePopTipOptimization { get; }

    public ConfigEntry<float> PopTipAggregationWindowMs { get; }

    public ConfigEntry<int> PopTipFastModeThreshold { get; }

    public ConfigEntry<bool> EnableTuJianPinyinSearch { get; }

    public ConfigEntry<bool> EnableFadeOptimization { get; }

    public ConfigEntry<float> FadeDurationScale { get; }

    public ConfigEntry<float> MapDoorTransitionSeconds { get; }

    public ConfigEntry<bool> EnableEasyBatchCompatibility { get; }

    public ConfigEntry<bool> EnableWhiteZeCompatibility { get; }

    public ConfigEntry<bool> EnableVToolsCompatibility { get; }

    public ConfigEntry<bool> EnableDemoCommandRegistration { get; }

    public ConfigEntry<bool> EnableDemoQueryRegistration { get; }

    public ConfigEntry<bool> EnableDemoMapRegistration { get; }

    public ConfigEntry<bool> EnableJsonModDemoInstall { get; }

    public ConfigEntry<string> JsonModDemoPath { get; }

    public ConfigEntry<string> ContentBackend { get; }
}
