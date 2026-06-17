using System;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveNativeProbeInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly LongLiveHostOptions _options;

    public LongLiveNativeProbeInstaller(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Name => "LongLiveNativeProbeInstaller";

    public void Install()
    {
        if (!_options.EnableNativeProbe.Value)
        {
            LongLiveNativeProbeState.Current = LongLiveNativeProbeResult.Disabled();
            return;
        }

        var libraryPath = ResolveLibraryPath();
        if (!File.Exists(libraryPath))
        {
            LongLiveNativeProbeState.Current = LongLiveNativeProbeResult.Failure(libraryPath, "Native library file does not exist.");
            _logger.LogInfo($"LongLive native probe skipped because the library does not exist: {libraryPath}");
            return;
        }

        try
        {
            var abiVersion = LongLiveNativeProbeBridge.GetAbiVersion(libraryPath);
            var ready = LongLiveNativeProbeBridge.IsReady(libraryPath);
            var sampleDamage = LongLiveNativeProbeBridge.ComputeTurnDamage(libraryPath, 180, 135, 24, 70, 18);
            var sum = LongLiveNativeProbeBridge.Add(libraryPath, 12, 30);

            LongLiveNativeProbeState.Current = LongLiveNativeProbeResult.CreateSuccess(libraryPath, abiVersion, ready, sum, sampleDamage);
            _logger.LogInfo($"LongLive native probe succeeded. abi={abiVersion}, ready={ready}, sum={sum}, turnDamage={sampleDamage}, path={libraryPath}");
        }
        catch (Exception exception)
        {
            LongLiveNativeProbeState.Current = LongLiveNativeProbeResult.Failure(libraryPath, exception.Message);
            _logger.LogError($"LongLive native probe failed: {exception}");
        }
    }

    private string ResolveLibraryPath()
    {
        if (!string.IsNullOrWhiteSpace(_options.NativeLibraryPath.Value))
        {
            return Path.GetFullPath(_options.NativeLibraryPath.Value);
        }

        var pluginDirectory = Path.GetDirectoryName(typeof(LongLivePlugin).Assembly.Location) ?? AppContext.BaseDirectory;
        var pluginLocalPath = Path.Combine(pluginDirectory, "longlive_native_core.dll");
        if (File.Exists(pluginLocalPath))
        {
            return pluginLocalPath;
        }

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "native", "target", "debug", "longlive_native_core.dll");
    }
}

internal static class LongLiveNativeProbeState
{
    public static LongLiveNativeProbeResult Current { get; set; } = LongLiveNativeProbeResult.Disabled();
}

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

internal static class LongLiveNativeProbeBridge
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int AbiVersionDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int AddDelegate(int left, int right);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int IsReadyDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ComputeTurnDamageDelegate(int attack, int skillPowerPercent, int flatBonus, int defense, int reductionPercent);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    public static int GetAbiVersion(string libraryPath)
    {
        using (var library = NativeLibraryHandle.Load(libraryPath))
        {
            return library.GetDelegate<AbiVersionDelegate>("longlive_native_core_abi_version")();
        }
    }

    public static int Add(string libraryPath, int left, int right)
    {
        using (var library = NativeLibraryHandle.Load(libraryPath))
        {
            return library.GetDelegate<AddDelegate>("longlive_native_core_add")(left, right);
        }
    }

    public static int IsReady(string libraryPath)
    {
        using (var library = NativeLibraryHandle.Load(libraryPath))
        {
            return library.GetDelegate<IsReadyDelegate>("longlive_native_core_is_ready")();
        }
    }

    public static int ComputeTurnDamage(string libraryPath, int attack, int skillPowerPercent, int flatBonus, int defense, int reductionPercent)
    {
        using (var library = NativeLibraryHandle.Load(libraryPath))
        {
            return library.GetDelegate<ComputeTurnDamageDelegate>("longlive_native_core_compute_turn_damage")(attack, skillPowerPercent, flatBonus, defense, reductionPercent);
        }
    }

    private sealed class NativeLibraryHandle : IDisposable
    {
        private IntPtr _handle;

        private NativeLibraryHandle(IntPtr handle)
        {
            _handle = handle;
        }

        public static NativeLibraryHandle Load(string libraryPath)
        {
            var handle = LoadLibrary(libraryPath);
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"LoadLibrary failed for native path: {libraryPath}, win32={Marshal.GetLastWin32Error()}");
            }

            return new NativeLibraryHandle(handle);
        }

        public TDelegate GetDelegate<TDelegate>(string exportName)
            where TDelegate : class
        {
            var proc = GetProcAddress(_handle, exportName);
            if (proc == IntPtr.Zero)
            {
                throw new InvalidOperationException($"GetProcAddress failed for export: {exportName}, win32={Marshal.GetLastWin32Error()}");
            }

            return Marshal.GetDelegateForFunctionPointer(proc, typeof(TDelegate)) as TDelegate
                ?? throw new InvalidOperationException($"Could not bind native export delegate: {exportName}");
        }

        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            FreeLibrary(_handle);
            _handle = IntPtr.Zero;
        }
    }
}
