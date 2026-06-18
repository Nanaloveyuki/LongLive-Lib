using System;
using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

internal sealed class LongLiveBattleDamagePipeline
{
    private readonly IReadOnlyList<ILongLiveBattleDamageMiddleware> _middlewares;

    public LongLiveBattleDamagePipeline(IReadOnlyList<ILongLiveBattleDamageMiddleware> middlewares)
    {
        _middlewares = middlewares ?? throw new ArgumentNullException(nameof(middlewares));
    }

    public LongLiveBattleDamageSegmentDecision Evaluate(LongLiveBattleDamageSegmentContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var decision = new LongLiveBattleDamageSegmentDecision(context.CurrentHp, context.IncomingDamage);
        foreach (var middleware in _middlewares)
        {
            middleware.Apply(context, decision);
        }

        return decision;
    }
}
