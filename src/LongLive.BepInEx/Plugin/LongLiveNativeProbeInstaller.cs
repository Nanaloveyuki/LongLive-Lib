using System;
using BepInEx.Logging;
using LongLive.BepInEx.Native;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveNativeProbeInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveNativeService _native;
    private readonly LongLiveHostOptions _options;

    public LongLiveNativeProbeInstaller(ManualLogSource logger, LongLiveNativeService native, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _native = native ?? throw new ArgumentNullException(nameof(native));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveNativeProbeInstaller";

    public void Install()
    {
        if (!_options.EnableNativeProbe.Value)
        {
            _logger.LogInfo("LongLive native probe is disabled by configuration.");
            return;
        }

        var result = _native.Probe(_options.NativeLibraryPath.Value);
        if (result.Success)
        {
            _logger.LogInfo($"LongLive native probe succeeded. abi={result.AbiVersion}, ready={result.ReadyFlag}, sum={result.Sum}, turnDamage={result.TurnDamage}, path={result.LibraryPath}");
            return;
        }
        _logger.LogInfo($"LongLive native probe failed: {result.Summary}, path={result.LibraryPath}");
    }
}
