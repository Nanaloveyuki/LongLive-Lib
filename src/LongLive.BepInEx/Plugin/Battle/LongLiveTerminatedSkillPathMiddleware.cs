namespace LongLive.BepInEx.Plugin;

internal sealed class LongLiveTerminatedSkillPathMiddleware : ILongLiveBattleDamageMiddleware
{
    public void Apply(LongLiveBattleDamageSegmentContext context, LongLiveBattleDamageSegmentDecision decision)
    {
        if (decision.SkipOriginalDamageInvocation || context.IsPlayerTarget || !context.IsSkillPathTerminated)
        {
            return;
        }

        var overflowDamage = context.IncomingDamage > 0 ? context.IncomingDamage : 0;
        decision.AppliedDamage = 0;
        decision.OverflowDamage = overflowDamage;
        decision.PredictedHpAfterSegment = context.CurrentHp;
        decision.IsLethal = true;
        decision.SkipOriginalDamageInvocation = true;
        decision.SkipRemainingSegments = true;
        decision.MarkSkillAsGuarded = context.SkillId > 0;
        decision.Reason = "terminated-skill-path";
    }
}
