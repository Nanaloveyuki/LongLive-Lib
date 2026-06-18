using System;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveMapSnapshotInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveMapSnapshotInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveMapSnapshotInstaller";

    public void Install()
    {
        _logger.LogInfo(
            $"LongLive map snapshot installer state: autoExport={_options.EnableAutoExportMapSnapshot.Value}, debug={_options.EnableDebugLogging.Value}");

        if (!_options.EnableAutoExportMapSnapshot.Value)
        {
            return;
        }

        if (!_options.EnableDebugLogging.Value)
        {
            _logger.LogInfo("LongLive map snapshot auto-export is configured, but debug logging is disabled. Skipping auto-export.");
            return;
        }

        _logger.LogInfo("LongLive map snapshot auto-export is armed and will run on a later scene-load when a non-empty snapshot becomes available.");
    }
}
