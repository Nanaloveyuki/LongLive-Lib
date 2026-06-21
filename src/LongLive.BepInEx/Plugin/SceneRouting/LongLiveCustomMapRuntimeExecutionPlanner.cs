using System;
using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCustomMapRuntimeExecutionPlanner
{
    public static LongLiveCustomMapRuntimeExecutionPlan Build(LongLiveCustomMapRuntimeActivationPlan activationPlan)
    {
        if (activationPlan is null)
        {
            throw new ArgumentNullException(nameof(activationPlan));
        }

        var steps = new List<LongLiveCustomMapRuntimeExecutionStep>();
        foreach (var target in activationPlan.Targets)
        {
            if (!target.RequiresCustomActivation)
            {
                steps.Add(new LongLiveCustomMapRuntimeExecutionStep
                {
                    StepId = "runtime-observe:" + target.SceneLogicalId,
                    SceneLogicalId = target.SceneLogicalId,
                    OwningModId = target.OwningModId,
                    Action = "observe-existing-runtime-shape",
                    Summary = "Observe the current host runtime shape and return flow before future activation work.",
                    IsDryRunOnly = true,
                });
                continue;
            }

            steps.Add(new LongLiveCustomMapRuntimeExecutionStep
            {
                StepId = "runtime-prepare-activation:" + target.SceneLogicalId,
                SceneLogicalId = target.SceneLogicalId,
                OwningModId = target.OwningModId,
                Action = "prepare-custom-runtime-activation",
                Summary = "Prepare host-side runtime activation for the custom scene target.",
                IsDryRunOnly = true,
            });

            steps.Add(new LongLiveCustomMapRuntimeExecutionStep
            {
                StepId = "runtime-bind-return:" + target.SceneLogicalId,
                SceneLogicalId = target.SceneLogicalId,
                OwningModId = target.OwningModId,
                Action = "bind-runtime-return-flow",
                Summary = "Bind the precomputed return scene and entry rules for the custom runtime target.",
                IsDryRunOnly = true,
            });
        }

        return new LongLiveCustomMapRuntimeExecutionPlan
        {
            StepCount = steps.Count,
            ActivationStepCount = CountByAction(steps, static step => step.Action.IndexOf("prepare", StringComparison.Ordinal) >= 0 || step.Action.IndexOf("bind", StringComparison.Ordinal) >= 0),
            DryRunStepCount = CountByAction(steps, static step => step.IsDryRunOnly),
            Steps = steps,
        };
    }

    private static int CountByAction(IReadOnlyList<LongLiveCustomMapRuntimeExecutionStep> steps, Func<LongLiveCustomMapRuntimeExecutionStep, bool> predicate)
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
