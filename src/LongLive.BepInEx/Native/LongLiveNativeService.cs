using System;
using System.IO;
using BepInEx.Logging;

namespace LongLive.BepInEx.Native;

public sealed class LongLiveNativeService
{
    private readonly ManualLogSource _logger;

    public LongLiveNativeService(ManualLogSource logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public LongLiveNativeProbeResult CurrentProbeResult { get; private set; } = LongLiveNativeProbeResult.Disabled();

    public LongLiveNativeProbeResult Probe(string configuredLibraryPath)
    {
        var libraryPath = LongLiveNativeLibraryResolver.ResolveLibraryPath(configuredLibraryPath);
        if (!File.Exists(libraryPath))
        {
            CurrentProbeResult = LongLiveNativeProbeResult.Failure(libraryPath, "Native library file does not exist.");
            return CurrentProbeResult;
        }

        try
        {
            var abiVersion = LongLiveNativeBridge.GetAbiVersion(libraryPath);
            var ready = LongLiveNativeBridge.IsReady(libraryPath);
            var sampleDamage = LongLiveNativeBridge.ComputeTurnDamage(libraryPath, 180, 135, 24, 70, 18);
            var sum = LongLiveNativeBridge.Add(libraryPath, 12, 30);

            CurrentProbeResult = LongLiveNativeProbeResult.CreateSuccess(libraryPath, abiVersion, ready, sum, sampleDamage);
            return CurrentProbeResult;
        }
        catch (Exception exception)
        {
            _logger.LogError($"LongLive native service probe failed: {exception}");
            CurrentProbeResult = LongLiveNativeProbeResult.Failure(libraryPath, exception.Message);
            return CurrentProbeResult;
        }
    }

    public int Add(string configuredLibraryPath, int left, int right)
    {
        var libraryPath = LongLiveNativeLibraryResolver.ResolveLibraryPath(configuredLibraryPath);
        return LongLiveNativeBridge.Add(libraryPath, left, right);
    }

    public int ComputeTurnDamage(string configuredLibraryPath, int attack, int skillPowerPercent, int flatBonus, int defense, int reductionPercent)
    {
        var libraryPath = LongLiveNativeLibraryResolver.ResolveLibraryPath(configuredLibraryPath);
        return LongLiveNativeBridge.ComputeTurnDamage(libraryPath, attack, skillPowerPercent, flatBonus, defense, reductionPercent);
    }

    public bool TryAdjudicateDamageSegment(string configuredLibraryPath, LongLiveNativeDamageSegmentRequest request, out LongLiveNativeDamageSegmentDecision decision)
    {
        var libraryPath = LongLiveNativeLibraryResolver.ResolveLibraryPath(configuredLibraryPath);
        if (!File.Exists(libraryPath))
        {
            decision = default;
            return false;
        }

        try
        {
            decision = LongLiveNativeBridge.AdjudicateDamageSegment(libraryPath, request);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogWarning($"LongLive native damage adjudication fallback: {exception.Message}");
            decision = default;
            return false;
        }
    }
}
