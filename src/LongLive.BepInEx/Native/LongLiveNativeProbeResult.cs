namespace LongLive.BepInEx.Native;

public sealed class LongLiveNativeProbeResult
{
    private LongLiveNativeProbeResult(bool enabled, bool success, string? libraryPath, string summary, int? abiVersion, int? readyFlag, int? sum, int? turnDamage)
    {
        Enabled = enabled;
        Success = success;
        LibraryPath = libraryPath;
        Summary = summary;
        AbiVersion = abiVersion;
        ReadyFlag = readyFlag;
        Sum = sum;
        TurnDamage = turnDamage;
    }

    public bool Enabled { get; }

    public bool Success { get; }

    public string? LibraryPath { get; }

    public string Summary { get; }

    public int? AbiVersion { get; }

    public int? ReadyFlag { get; }

    public int? Sum { get; }

    public int? TurnDamage { get; }

    public static LongLiveNativeProbeResult Disabled()
    {
        return new LongLiveNativeProbeResult(false, false, null, "disabled", null, null, null, null);
    }

    public static LongLiveNativeProbeResult CreateSuccess(string libraryPath, int abiVersion, int readyFlag, int sum, int turnDamage)
    {
        return new LongLiveNativeProbeResult(true, true, libraryPath, "success", abiVersion, readyFlag, sum, turnDamage);
    }

    public static LongLiveNativeProbeResult Failure(string libraryPath, string message)
    {
        return new LongLiveNativeProbeResult(true, false, libraryPath, message, null, null, null, null);
    }
}
