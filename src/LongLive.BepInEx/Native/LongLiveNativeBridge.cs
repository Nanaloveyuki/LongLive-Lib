using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace LongLive.BepInEx.Native;

internal static class LongLiveNativeBridge
{
    private static readonly object SyncRoot = new object();
    private static readonly Dictionary<string, NativeExports> ExportCache = new Dictionary<string, NativeExports>(StringComparer.OrdinalIgnoreCase);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int AbiVersionDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int AddDelegate(int left, int right);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int IsReadyDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ComputeTurnDamageDelegate(int attack, int skillPowerPercent, int flatBonus, int defense, int reductionPercent);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate LongLiveNativeDamageSegmentDecision AdjudicateDamageSegmentDelegate(LongLiveNativeDamageSegmentRequest request);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    public static int GetAbiVersion(string libraryPath)
    {
        return GetExports(libraryPath).AbiVersion();
    }

    public static int Add(string libraryPath, int left, int right)
    {
        return GetExports(libraryPath).Add(left, right);
    }

    public static int IsReady(string libraryPath)
    {
        return GetExports(libraryPath).IsReady();
    }

    public static int ComputeTurnDamage(string libraryPath, int attack, int skillPowerPercent, int flatBonus, int defense, int reductionPercent)
    {
        return GetExports(libraryPath).ComputeTurnDamage(attack, skillPowerPercent, flatBonus, defense, reductionPercent);
    }

    public static LongLiveNativeDamageSegmentDecision AdjudicateDamageSegment(string libraryPath, LongLiveNativeDamageSegmentRequest request)
    {
        return GetExports(libraryPath).AdjudicateDamageSegment(request);
    }

    public static void ClearCache()
    {
        lock (SyncRoot)
        {
            foreach (var exports in ExportCache.Values)
            {
                exports.Dispose();
            }

            ExportCache.Clear();
        }
    }

    private static NativeExports GetExports(string libraryPath)
    {
        var fullPath = Path.GetFullPath(libraryPath);

        lock (SyncRoot)
        {
            if (ExportCache.TryGetValue(fullPath, out var cached))
            {
                return cached;
            }

            var loaded = NativeExports.Load(fullPath);
            ExportCache.Add(fullPath, loaded);
            return loaded;
        }
    }

    private sealed class NativeExports : IDisposable
    {
        private readonly NativeLibraryHandle _library;

        private NativeExports(
            NativeLibraryHandle library,
            AbiVersionDelegate abiVersion,
            AddDelegate add,
            IsReadyDelegate isReady,
            ComputeTurnDamageDelegate computeTurnDamage,
            AdjudicateDamageSegmentDelegate adjudicateDamageSegment)
        {
            _library = library;
            AbiVersion = abiVersion;
            Add = add;
            IsReady = isReady;
            ComputeTurnDamage = computeTurnDamage;
            AdjudicateDamageSegment = adjudicateDamageSegment;
        }

        public AbiVersionDelegate AbiVersion { get; }

        public AddDelegate Add { get; }

        public IsReadyDelegate IsReady { get; }

        public ComputeTurnDamageDelegate ComputeTurnDamage { get; }

        public AdjudicateDamageSegmentDelegate AdjudicateDamageSegment { get; }

        public static NativeExports Load(string libraryPath)
        {
            var library = NativeLibraryHandle.Load(libraryPath);
            try
            {
                return new NativeExports(
                    library,
                    library.GetDelegate<AbiVersionDelegate>("longlive_native_core_abi_version"),
                    library.GetDelegate<AddDelegate>("longlive_native_core_add"),
                    library.GetDelegate<IsReadyDelegate>("longlive_native_core_is_ready"),
                    library.GetDelegate<ComputeTurnDamageDelegate>("longlive_native_core_compute_turn_damage"),
                    library.GetDelegate<AdjudicateDamageSegmentDelegate>("longlive_native_core_adjudicate_damage_segment"));
            }
            catch
            {
                library.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            _library.Dispose();
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
