using LongLive.BepInEx.Native;

namespace LongLive.BepInEx.Plugin;

internal sealed class LongLiveNativeDamageAdjudicationMiddleware : ILongLiveBattleDamageMiddleware
{
    public void Apply(LongLiveBattleDamageSegmentContext context, LongLiveBattleDamageSegmentDecision decision)
    {
        if (decision.SkipOriginalDamageInvocation || context.IsPlayerTarget || context.CurrentHp <= 0 || context.IncomingDamage <= 0)
        {
            return;
        }

        var plugin = LongLivePlugin.Instance;
        if (plugin == null)
        {
            return;
        }

        var request = new LongLiveNativeDamageSegmentRequest
        {
            CurrentHp = context.CurrentHp,
            IncomingDamage = context.IncomingDamage,
            SkillId = context.SkillId,
            DamageType = context.DamageType,
            IsPlayerTarget = context.IsPlayerTarget ? 1 : 0,
            IsMultiHit = context.IsMultiHit ? 1 : 0,
            SegmentIndex = context.SegmentIndex
        };

        if (!plugin.Native.TryAdjudicateDamageSegment(plugin.Options.NativeLibraryPath.Value, request, out var nativeDecision))
        {
            return;
        }

        decision.AppliedDamage = nativeDecision.AppliedDamage;
        decision.OverflowDamage = nativeDecision.OverflowDamage;
        decision.PredictedHpAfterSegment = nativeDecision.PredictedHpAfterSegment;
        decision.IsLethal = nativeDecision.IsLethal;
        decision.MarkSkillAsLethalCandidate = nativeDecision.IsLethal && context.SkillId > 0;

        if (!nativeDecision.IsLethal)
        {
            decision.SkipOriginalDamageInvocation = nativeDecision.ShouldSkipOriginalDamage;
            decision.SkipRemainingSegments = nativeDecision.ShouldSkipRemainingSegments;
            decision.ClampResultHpToZero = nativeDecision.ShouldClampResultHpToZero;
            decision.MarkSkillAsGuarded = decision.MarkSkillAsGuarded || (context.SkillId > 0 && nativeDecision.ShouldSkipRemainingSegments);
        }

        decision.NativeDecisionApplied = true;
        decision.Reason = nativeDecision.IsLethal ? "native-lethal" : "native-pass";
    }
}
