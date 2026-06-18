using System;
using System.IO;
using System.Runtime.InteropServices;

internal static class Program
{
    private static int Main(string[] args)
    {
        var libraryPath = args.Length > 0
            ? Path.GetFullPath(args[0])
            : GetDefaultLibraryPath();

        if (!File.Exists(libraryPath))
        {
            Console.Error.WriteLine($"Native library not found: {libraryPath}");
            return 2;
        }

        NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, (name, assembly, searchPath) =>
        {
            if (!string.Equals(name, LongLiveNative.LibraryName, StringComparison.Ordinal))
            {
                return IntPtr.Zero;
            }

            return NativeLibrary.Load(libraryPath);
        });

        var abiVersion = LongLiveNative.GetAbiVersion();
        var sum = LongLiveNative.Add(12, 30);
        var ready = LongLiveNative.IsReady();
        var turnDamage = LongLiveNative.ComputeTurnDamage(
            attack: 180,
            skillPowerPercent: 135,
            flatBonus: 24,
            defense: 70,
            reductionPercent: 18);
        var segmentDecision = LongLiveNative.AdjudicateDamageSegment(new LongLiveNative.LongLiveNativeDamageSegmentRequest
        {
            CurrentHp = 280,
            IncomingDamage = 134427,
            SkillId = 7074,
            DamageType = 0,
            IsPlayerTarget = 0,
            IsMultiHit = 1,
            SegmentIndex = 0
        });

        Console.WriteLine($"abi_version={abiVersion}");
        Console.WriteLine($"sum={sum}");
        Console.WriteLine($"ready={ready}");
        Console.WriteLine($"turn_damage={turnDamage}");
        Console.WriteLine($"segment_applied={segmentDecision.AppliedDamage}");
        Console.WriteLine($"segment_overflow={segmentDecision.OverflowDamage}");
        Console.WriteLine($"segment_predicted_hp={segmentDecision.PredictedHpAfterSegment}");
        Console.WriteLine($"segment_flags={segmentDecision.Flags}");
        return 0;
    }

    private static string GetDefaultLibraryPath()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "native", "target", "debug", "longlive_native_core.dll");
    }
}

internal static class LongLiveNative
{
    public const string LibraryName = "longlive_native_core";

    [StructLayout(LayoutKind.Sequential)]
    public struct LongLiveNativeDamageSegmentRequest
    {
        public int CurrentHp;
        public int IncomingDamage;
        public int SkillId;
        public int DamageType;
        public int IsPlayerTarget;
        public int IsMultiHit;
        public int SegmentIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LongLiveNativeDamageSegmentDecision
    {
        public int AppliedDamage;
        public int OverflowDamage;
        public int PredictedHpAfterSegment;
        public int Flags;
    }

    [DllImport(LibraryName, EntryPoint = "longlive_native_core_abi_version", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetAbiVersion();

    [DllImport(LibraryName, EntryPoint = "longlive_native_core_add", CallingConvention = CallingConvention.Cdecl)]
    public static extern int Add(int left, int right);

    [DllImport(LibraryName, EntryPoint = "longlive_native_core_is_ready", CallingConvention = CallingConvention.Cdecl)]
    public static extern int IsReady();

    [DllImport(LibraryName, EntryPoint = "longlive_native_core_compute_turn_damage", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ComputeTurnDamage(
        int attack,
        int skillPowerPercent,
        int flatBonus,
        int defense,
        int reductionPercent);

    [DllImport(LibraryName, EntryPoint = "longlive_native_core_adjudicate_damage_segment", CallingConvention = CallingConvention.Cdecl)]
    public static extern LongLiveNativeDamageSegmentDecision AdjudicateDamageSegment(LongLiveNativeDamageSegmentRequest request);
}
