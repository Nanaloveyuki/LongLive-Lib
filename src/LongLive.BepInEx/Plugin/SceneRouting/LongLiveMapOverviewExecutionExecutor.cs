using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapOverviewExecutionExecutor
{
    public static LongLiveMapOverviewExecutionReport ExecuteDryRun(LongLiveMapOverviewExecutionPlan plan, ManualLogSource logger)
    {
        return ExecuteDryRun(plan, logger, logSteps: true);
    }

    public static LongLiveMapOverviewExecutionReport ExecuteDryRun(LongLiveMapOverviewExecutionPlan plan, ManualLogSource logger, bool logSteps)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var probeCaptured = LongLiveMapOverviewHostProbe.TryCapture(out var probe);
        var results = new List<LongLiveMapOverviewExecutionStepResult>();
        if (logSteps)
        {
            logger.LogInfo($"[MapOverviewExecutor] dry-run plan: steps={plan.StepCount}, injections={plan.InjectionStepCount}, dryRun={plan.DryRunStepCount}");
        }

        foreach (var step in plan.Steps)
        {
            var result = ExecuteStep(step, probeCaptured, probe);
            results.Add(result);
            if (logSteps)
            {
                logger.LogInfo($"[MapOverviewExecutor] step={step.StepId}, page={step.PageId}, mod={step.OwningModId}, action={step.Action}, dryRun={step.IsDryRunOnly}, status={result.StatusCode}, success={result.Succeeded}, detail={result.Detail}");
            }
        }

        return new LongLiveMapOverviewExecutionReport
        {
            StepCount = results.Count,
            SuccessCount = CountBy(results, static result => result.Succeeded),
            FailureCount = CountBy(results, static result => !result.Succeeded),
            HostProbe = probe,
            Results = results,
        };
    }

    private static LongLiveMapOverviewExecutionStepResult ExecuteStep(LongLiveMapOverviewExecutionStep step, bool probeCaptured, LongLiveMapOverviewHostProbeSnapshot probe)
    {
        if (!probeCaptured)
        {
            return new LongLiveMapOverviewExecutionStepResult
            {
                StepId = step.StepId,
                Action = step.Action,
                Succeeded = false,
                StatusCode = "host-probe-failed",
                Detail = string.IsNullOrWhiteSpace(probe.ProbeError) ? "Map overview host probe failed." : probe.ProbeError,
                HostBinding = new LongLiveMapOverviewHostBindingSnapshot
                {
                    PageId = step.PageId,
                    OwningModId = step.OwningModId,
                },
            };
        }

        var expectsSea = step.PageId.IndexOf("sea", StringComparison.OrdinalIgnoreCase) >= 0;
        var hasExpectedHostRoot = expectsSea ? probe.HasSea : probe.HasNingZhou;
        var hasInjectionAnchor = expectsSea ? probe.HasSeaInjectionAnchor : probe.HasNingZhouInjectionAnchor;
        var binding = BuildBinding(step, probe, expectsSea, hasExpectedHostRoot, hasInjectionAnchor);
        return new LongLiveMapOverviewExecutionStepResult
        {
            StepId = step.StepId,
            Action = step.Action,
            Succeeded = probe.HasUiMapPanel && hasExpectedHostRoot && hasInjectionAnchor,
            StatusCode = probe.HasUiMapPanel && hasExpectedHostRoot && hasInjectionAnchor ? "preflight-ok" : hasExpectedHostRoot ? "missing-injection-anchor" : "missing-host-root",
            Detail = expectsSea
                ? $"HasUiMapPanel={probe.HasUiMapPanel}, HasSea={probe.HasSea}, HasSeaAnchor={probe.HasSeaInjectionAnchor}, SeaNodeRoot={probe.SeaNodeRootName}, SeaHighlightRoot={probe.SeaHighlightRootName}"
                : $"HasUiMapPanel={probe.HasUiMapPanel}, HasNingZhou={probe.HasNingZhou}, HasNingZhouAnchor={probe.HasNingZhouInjectionAnchor}, NingZhouNodeRoot={probe.NingZhouNodeRootName}, NingZhouHighlightRoot={probe.NingZhouHighlightRootName}",
            HostBinding = binding,
        };
    }

    private static LongLiveMapOverviewHostBindingSnapshot BuildBinding(LongLiveMapOverviewExecutionStep step, LongLiveMapOverviewHostProbeSnapshot probe, bool expectsSea, bool hasExpectedHostRoot, bool hasInjectionAnchor)
    {
        return new LongLiveMapOverviewHostBindingSnapshot
        {
            PageId = step.PageId,
            OwningModId = step.OwningModId,
            ExpectsSeaHost = expectsSea,
            HasExpectedHostRoot = hasExpectedHostRoot,
            HasInjectionAnchor = hasInjectionAnchor,
            InjectionAnchorName = expectsSea ? probe.SeaInjectionAnchorName : probe.NingZhouInjectionAnchorName,
            HostRootName = expectsSea ? probe.SeaNodeRootName : probe.NingZhouNodeRootName,
            HighlightRootName = expectsSea ? probe.SeaHighlightRootName : probe.NingZhouHighlightRootName,
            NodeChildCount = expectsSea ? probe.SeaNodeChildCount : probe.NingZhouNodeChildCount,
            HighlightChildCount = expectsSea ? probe.SeaHighlightChildCount : probe.NingZhouHighlightChildCount,
            HierarchySample = expectsSea ? probe.SeaHierarchySample : probe.NingZhouHierarchySample,
        };
    }

    private static int CountBy(IReadOnlyList<LongLiveMapOverviewExecutionStepResult> results, Func<LongLiveMapOverviewExecutionStepResult, bool> predicate)
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
