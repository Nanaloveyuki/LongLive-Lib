using System.Runtime.InteropServices;

namespace LongLive.BepInEx.Native;

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
