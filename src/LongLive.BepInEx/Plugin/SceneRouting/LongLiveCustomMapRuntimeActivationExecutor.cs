using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveCustomMapRuntimeActivationExecutor
{
    public static LongLiveCustomMapRuntimeActivationExecutionReport ExecutePlan(LongLiveCustomMapRuntimeActivationExecutionPlan plan, ManualLogSource logger, bool logSteps)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var runtime = LongLivePluginContext.GetCustomMapRuntimeActivationRuntimeSnapshot(int.MaxValue);
        var results = new List<LongLiveCustomMapRuntimeActivationStepResult>();
        if (logSteps)
        {
            logger.LogInfo($"[CustomMapRuntimeActivationExecutor] plan: steps={plan.StepCount}, executable={plan.ExecutableStepCount}, pending={plan.PendingStepCount}");
        }

        foreach (var step in plan.Steps)
        {
            var result = ExecuteStep(step, runtime);
            results.Add(result);
            if (logSteps)
            {
                logger.LogInfo($"[CustomMapRuntimeActivationExecutor] step={step.StepId}, scene={step.SceneLogicalId}, action={step.Action}, executable={step.IsExecutable}, status={result.StatusCode}, success={result.Succeeded}, detail={result.Detail}");
            }
        }

        var report = new LongLiveCustomMapRuntimeActivationExecutionReport
        {
            StepCount = results.Count,
            SuccessCount = CountBy(results, static result => result.Succeeded),
            FailureCount = CountBy(results, static result => !result.Succeeded),
            Results = results,
        };
        LongLiveCustomMapRuntimeActivationArtifactRegistry.ApplyReport(report, runtime);

        return report;
    }

    private static LongLiveCustomMapRuntimeActivationStepResult ExecuteStep(LongLiveCustomMapRuntimeActivationStep step, LongLiveCustomMapRuntimeActivationRuntimeSnapshot runtime)
    {
        var target = FindTarget(runtime, step.SceneLogicalId);
        if (target is null)
        {
            return new LongLiveCustomMapRuntimeActivationStepResult
            {
                StepId = step.StepId,
                SceneLogicalId = step.SceneLogicalId,
                Action = step.Action,
                Succeeded = false,
                StatusCode = "missing-activation-target",
                Detail = "The activation target was not found in the current runtime activation snapshot.",
                ActivationState = string.Empty,
            };
        }

        if (!step.IsExecutable)
        {
            return new LongLiveCustomMapRuntimeActivationStepResult
            {
                StepId = step.StepId,
                SceneLogicalId = step.SceneLogicalId,
                Action = step.Action,
                Succeeded = false,
                StatusCode = "activation-step-pending",
                Detail = target.Detail,
                ActivationState = target.ActivationState,
                ProducedHostPreparation = false,
                ProducedProxyBinding = false,
            };
        }

        if (string.Equals(step.Action, "prepare-runtime-activation-host-surface", StringComparison.Ordinal))
        {
            return new LongLiveCustomMapRuntimeActivationStepResult
            {
                StepId = step.StepId,
                SceneLogicalId = step.SceneLogicalId,
                Action = step.Action,
                Succeeded = true,
                StatusCode = "activation-prepared-proxy",
                Detail = "LongLive confirmed that the current host-backed proxy scene can act as a temporary activation surface. No real host mutation is applied yet.",
                ActivationState = target.ActivationState,
                ProducedHostPreparation = true,
                ProducedProxyBinding = false,
            };
        }

        if (string.Equals(step.Action, "bind-runtime-activation-route", StringComparison.Ordinal))
        {
            return new LongLiveCustomMapRuntimeActivationStepResult
            {
                StepId = step.StepId,
                SceneLogicalId = step.SceneLogicalId,
                Action = step.Action,
                Succeeded = true,
                StatusCode = "activation-route-bound-proxy",
                Detail = "LongLive confirmed that the current proxy route can be bound as a placeholder activation route. The target still reuses a host scene and has not become an independent runtime yet.",
                ActivationState = target.ActivationState,
                ProducedHostPreparation = true,
                ProducedProxyBinding = true,
            };
        }

        return new LongLiveCustomMapRuntimeActivationStepResult
        {
            StepId = step.StepId,
            SceneLogicalId = step.SceneLogicalId,
            Action = step.Action,
            Succeeded = true,
            StatusCode = "activation-observed",
            Detail = target.Detail,
            ActivationState = target.ActivationState,
            ProducedHostPreparation = false,
            ProducedProxyBinding = false,
        };
    }

    private static LongLiveCustomMapRuntimeActivationRuntimeTarget? FindTarget(LongLiveCustomMapRuntimeActivationRuntimeSnapshot runtime, string sceneLogicalId)
    {
        foreach (var target in runtime.Targets)
        {
            if (string.Equals(target.SceneLogicalId, sceneLogicalId, StringComparison.Ordinal))
            {
                return target;
            }
        }

        return null;
    }

    private static int CountBy(IReadOnlyList<LongLiveCustomMapRuntimeActivationStepResult> results, Func<LongLiveCustomMapRuntimeActivationStepResult, bool> predicate)
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
