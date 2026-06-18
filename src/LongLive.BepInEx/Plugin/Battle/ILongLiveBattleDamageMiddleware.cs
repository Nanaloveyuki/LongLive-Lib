namespace LongLive.BepInEx.Plugin;

internal interface ILongLiveBattleDamageMiddleware
{
    void Apply(LongLiveBattleDamageSegmentContext context, LongLiveBattleDamageSegmentDecision decision);
}
