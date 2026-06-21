using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCustomMapRuntimeExecutionExecutor
{
    public static LongLiveCustomMapRuntimeExecutionReport ExecuteDryRun(LongLiveCustomMapRuntimeExecutionPlan plan, ManualLogSource logger)
    {
        return ExecuteDryRun(plan, logger, logSteps: true);
    }

    public static LongLiveCustomMapRuntimeExecutionReport ExecuteDryRun(LongLiveCustomMapRuntimeExecutionPlan plan, ManualLogSource logger, bool logSteps)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var results = new List<LongLiveCustomMapRuntimeExecutionStepResult>();
        if (logSteps)
        {
            logger.LogInfo($"[CustomMapRuntimeExecutor] dry-run plan: steps={plan.StepCount}, activationSteps={plan.ActivationStepCount}, dryRun={plan.DryRunStepCount}");
        }

        foreach (var step in plan.Steps)
        {
            var result = ExecuteStep(step);
            results.Add(result);
            if (logSteps)
            {
                logger.LogInfo($"[CustomMapRuntimeExecutor] step={step.StepId}, scene={step.SceneLogicalId}, mod={step.OwningModId}, action={step.Action}, dryRun={step.IsDryRunOnly}, status={result.StatusCode}, success={result.Succeeded}, detail={result.Detail}");
            }
        }

        return new LongLiveCustomMapRuntimeExecutionReport
        {
            StepCount = results.Count,
            SuccessCount = CountBy(results, static result => result.Succeeded),
            FailureCount = CountBy(results, static result => !result.Succeeded),
            Results = results,
        };
    }

    private static LongLiveCustomMapRuntimeExecutionStepResult ExecuteStep(LongLiveCustomMapRuntimeExecutionStep step)
    {
        var activationPlan = LongLivePluginContext.GetCustomMapRuntimeActivationPlan();
        var target = FindTarget(activationPlan, step.SceneLogicalId);
        if (target is null)
        {
            return new LongLiveCustomMapRuntimeExecutionStepResult
            {
                StepId = step.StepId,
                Action = step.Action,
                Succeeded = false,
                StatusCode = "missing-activation-target",
                Detail = "The requested activation target was not found in the current runtime activation plan.",
                HostBinding = new LongLiveCustomMapRuntimeHostBindingSnapshot
                {
                    SceneLogicalId = step.SceneLogicalId,
                    OwningModId = step.OwningModId,
                },
            };
        }

        var readiness = LongLiveCustomMapRuntimeReadinessEvaluator.EvaluateTarget(target);
        var binding = new LongLiveCustomMapRuntimeHostBindingSnapshot
        {
            SceneLogicalId = target.SceneLogicalId,
            SceneName = target.SceneName,
            OwningModId = target.OwningModId,
            EntryRouteResolvable = readiness.EntryRouteResolvable,
            ReturnRouteResolvable = readiness.ReturnRouteResolvable,
            ReturnRouteRequired = readiness.ReturnRouteRequired,
            PreferredEntryIndexSane = readiness.PreferredEntryIndexSane,
            PreferredReturnEntryIndexSane = readiness.PreferredReturnEntryIndexSane,
            EntryRouteKind = readiness.EntryRouteKind,
            ReturnRouteKind = readiness.ReturnRouteKind,
            TopologyNodeCount = readiness.TopologyNodeCount,
            EntrySceneName = readiness.EntrySceneName,
            ReturnSceneName = readiness.ReturnSceneName,
        };

        return new LongLiveCustomMapRuntimeExecutionStepResult
        {
            StepId = step.StepId,
            Action = step.Action,
            Succeeded = readiness.CanEnterNow,
            StatusCode = readiness.StatusCode,
            Detail = readiness.Detail,
            HostBinding = binding,
        };
    }

    private static LongLiveCustomMapRuntimeActivationTarget? FindTarget(LongLiveCustomMapRuntimeActivationPlan plan, string sceneLogicalId)
    {
        foreach (var target in plan.Targets)
        {
            if (string.Equals(target.SceneLogicalId, sceneLogicalId, StringComparison.Ordinal))
            {
                return target;
            }
        }

        return null;
    }

    private static int CountBy(IReadOnlyList<LongLiveCustomMapRuntimeExecutionStepResult> results, Func<LongLiveCustomMapRuntimeExecutionStepResult, bool> predicate)
    {
        var count = 0;
        foreach (var result in results)
        {
            if (predicate(result))
            {
                count++;
            }
        }

        return count;
    }
}
