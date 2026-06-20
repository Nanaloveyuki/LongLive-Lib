using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveBulkItemUseInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

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

        _logger.LogInfo($"LongLive bulk item-use installer active. chunkSize={Math.Max(1, _options.BulkItemUseChunkSize.Value)}");
    }
}
