using System;
using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCustomMapRuntimeActivationPlanner
{
    public static LongLiveCustomMapRuntimeActivationExecutionPlan Build(LongLiveCustomMapRuntimeActivationRuntimeSnapshot runtime)
    {
        if (runtime is null)
        {
            throw new ArgumentNullException(nameof(runtime));
        }

        var steps = new List<LongLiveCustomMapRuntimeActivationStep>();
        foreach (var target in runtime.Targets)
        {
            steps.Add(new LongLiveCustomMapRuntimeActivationStep
            {
                StepId = "activation-observe:" + target.SceneLogicalId,
                SceneLogicalId = target.SceneLogicalId,
                OwningModId = target.OwningModId,
                Action = "observe-activation-state",
                Summary = "Observe the current runtime activation state before attempting host mutation.",
                IsExecutable = true,
            });

            if (string.Equals(target.ActivationState, "host-proxy", StringComparison.Ordinal)
                || string.Equals(target.ActivationState, "pending-implementation", StringComparison.Ordinal))
            {
                steps.Add(new LongLiveCustomMapRuntimeActivationStep
                {
                    StepId = "activation-prepare:" + target.SceneLogicalId,
                    SceneLogicalId = target.SceneLogicalId,
                    OwningModId = target.OwningModId,
                    Action = "prepare-runtime-activation-host-surface",
                    Summary = "Prepare the host-side runtime activation surface for a custom scene target.",
                    IsExecutable = target.CanPrepareHostActivationSurface,
                });

                steps.Add(new LongLiveCustomMapRuntimeActivationStep
                {
                    StepId = "activation-bind:" + target.SceneLogicalId,
                    SceneLogicalId = target.SceneLogicalId,
                    OwningModId = target.OwningModId,
                    Action = "bind-runtime-activation-route",
                    Summary = "Bind a concrete activation route that can replace the current proxy or blocked target.",
                    IsExecutable = target.CanBindProxyRoute,
                });
            }
        }

        return new LongLiveCustomMapRuntimeActivationExecutionPlan
        {
            StepCount = steps.Count,
            ExecutableStepCount = CountBy(steps, static step => step.IsExecutable),
            PendingStepCount = CountBy(steps, static step => !step.IsExecutable),
            Steps = steps,
        };
    }

    private static int CountBy(IReadOnlyList<LongLiveCustomMapRuntimeActivationStep> steps, Func<LongLiveCustomMapRuntimeActivationStep, bool> predicate)
    {
        var count = 0;
        foreach (var step in steps)
        {
            if (predicate(step))
            {
                count++;
            }
        }

        return count;
    }
}
