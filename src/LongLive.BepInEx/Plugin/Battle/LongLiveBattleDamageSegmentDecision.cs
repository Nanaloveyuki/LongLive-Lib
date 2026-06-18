namespace LongLive.BepInEx.Plugin;

internal sealed class LongLiveBattleDamageSegmentDecision
{
    public LongLiveBattleDamageSegmentDecision(int currentHp, int incomingDamage)
    {
        var normalizedDamage = incomingDamage > 0 ? incomingDamage : 0;
        AppliedDamage = normalizedDamage;
        OverflowDamage = 0;
        PredictedHpAfterSegment = currentHp - normalizedDamage;
        if (PredictedHpAfterSegment < 0)
        {
            PredictedHpAfterSegment = 0;
        }

        Reason = "pass-through";
    }

    public int AppliedDamage { get; set; }

    public int OverflowDamage { get; set; }

    public int PredictedHpAfterSegment { get; set; }

    public bool IsLethal { get; set; }

    public bool SkipOriginalDamageInvocation { get; set; }

    public bool SkipRemainingSegments { get; set; }

    public bool ClampResultHpToZero { get; set; }

    public bool MarkSkillAsGuarded { get; set; }

    public bool MarkSkillAsLethalCandidate { get; set; }

    public bool NativeDecisionApplied { get; set; }

    public string Reason { get; set; }
}
