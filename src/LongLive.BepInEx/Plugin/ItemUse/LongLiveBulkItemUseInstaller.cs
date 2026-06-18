using System;
using BepInEx.Logging;
using HarmonyLib;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveBulkItemUseInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;
    private static bool _easyBatchUpdatePatchInstalled;

    public LongLiveBulkItemUseInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveBulkItemUseInstaller";

    public void Install()
    {
        if (!_options.EnableBulkItemUseOptimization.Value)
        {
            return;
        }

        InstallEasyBatchUpdatePatch();
        _logger.LogInfo($"LongLive bulk item-use installer active. chunkSize={Math.Max(1, _options.BulkItemUseChunkSize.Value)}");
    }

    private void InstallEasyBatchUpdatePatch()
    {
        if (_easyBatchUpdatePatchInstalled)
        {
            return;
        }

        var targetType = AccessTools.TypeByName("EasyBatch.Plugin");
        var targetMethod = targetType == null ? null : AccessTools.Method(targetType, "Update", Type.EmptyTypes);
        if (targetMethod == null)
        {
            _logger.LogInfo("LongLive bulk item-use installer could not resolve EasyBatch.Plugin.Update. EasyBatch interception remains inactive.");
            return;
        }

        var prefix = AccessTools.Method(typeof(LongLiveBulkItemUseInstaller), nameof(EasyBatchUpdatePrefix));
        if (prefix == null)
        {
            _logger.LogWarning("LongLive bulk item-use installer could not resolve EasyBatchUpdatePrefix.");
            return;
        }

        var harmony = new Harmony(LongLivePluginMetadata.PluginGuid + ".bulkitemuse.easybatch");
        harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefix));
        _easyBatchUpdatePatchInstalled = true;
        _logger.LogInfo("LongLive bulk item-use installer disabled EasyBatch.Plugin.Update and will use LongLive-owned long-press handling.");
    }

    private static bool EasyBatchUpdatePrefix()
    {
        return false;
    }
}
