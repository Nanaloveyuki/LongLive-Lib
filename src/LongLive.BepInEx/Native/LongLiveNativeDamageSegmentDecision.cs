using System.Runtime.InteropServices;

namespace LongLive.BepInEx.Native;

[StructLayout(LayoutKind.Sequential)]
public struct LongLiveNativeDamageSegmentDecision
{
    public const int FlagLethal = 1;
    public const int FlagSkipOriginalDamage = 1 << 1;
    public const int FlagSkipRemainingSegments = 1 << 2;
    public const int FlagClampResultHpToZero = 1 << 3;

    public int AppliedDamage;

    public int OverflowDamage;

    public int PredictedHpAfterSegment;

    public int Flags;

    public bool IsLethal => (Flags & FlagLethal) != 0;

    public bool ShouldSkipOriginalDamage => (Flags & FlagSkipOriginalDamage) != 0;

    public bool ShouldSkipRemainingSegments => (Flags & FlagSkipRemainingSegments) != 0;

    public bool ShouldClampResultHpToZero => (Flags & FlagClampResultHpToZero) != 0;
}
