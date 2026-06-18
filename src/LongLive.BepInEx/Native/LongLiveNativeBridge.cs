using System;
using System.Runtime.InteropServices;

namespace LongLive.BepInEx.Native;

internal static class LongLiveNativeBridge
{
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

    public static LongLiveNativeDamageSegmentDecision AdjudicateDamageSegment(string libraryPath, LongLiveNativeDamageSegmentRequest request)
    {
        using (var library = NativeLibraryHandle.Load(libraryPath))
        {
            return library.GetDelegate<AdjudicateDamageSegmentDelegate>("longlive_native_core_adjudicate_damage_segment")(request);
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
