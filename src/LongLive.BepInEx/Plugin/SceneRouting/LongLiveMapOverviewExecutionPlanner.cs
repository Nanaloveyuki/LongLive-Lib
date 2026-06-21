using System;
using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapOverviewExecutionPlanner
{
    public static LongLiveMapOverviewExecutionPlan Build(LongLiveMapOverviewInstallPlan installPlan)
    {
        if (installPlan is null)
        {
            throw new ArgumentNullException(nameof(installPlan));
        }

        var shellAllocationPlan = LongLivePluginContext.GetMapOverviewShellAllocationPlan();

        var steps = new List<LongLiveMapOverviewExecutionStep>();
        foreach (var target in installPlan.PageTargets)
        {
            var allocationTarget = FindAllocationTarget(shellAllocationPlan, target.PageId);
            if (!target.RequiresHostInjection)
            {
                steps.Add(new LongLiveMapOverviewExecutionStep
                {
                    StepId = "overview-observe:" + target.PageId,
                    PageId = target.PageId,
                    OwningModId = target.OwningModId,
                    Action = "observe-existing-page-shape",
                    Summary = "Observe the current host overview page shape before any future injection work.",
                    IsDryRunOnly = true,
                });
                continue;
            }

            steps.Add(new LongLiveMapOverviewExecutionStep
            {
                StepId = "overview-install-page:" + target.PageId,
                PageId = target.PageId,
                OwningModId = target.OwningModId,
                Action = "allocate-custom-page-shell",
                Summary = allocationTarget is null
                    ? "Allocate a future host-side overview page shell for the target mod page."
                    : "Allocate a future host-side overview page shell for the target mod page. Reason: " + allocationTarget.AllocationReason,
                IsDryRunOnly = true,
            });

            steps.Add(new LongLiveMapOverviewExecutionStep
            {
                StepId = "overview-install-projections:" + target.PageId,
                PageId = target.PageId,
                OwningModId = target.OwningModId,
                Action = "install-node-projections",
                Summary = "Bind route projections and node metadata into the future host overview page shell.",
                IsDryRunOnly = true,
            });
        }

        return new LongLiveMapOverviewExecutionPlan
        {
            StepCount = steps.Count,
            InjectionStepCount = CountByAction(steps, static step => step.Action.IndexOf("install", StringComparison.Ordinal) >= 0),
            DryRunStepCount = CountByAction(steps, static step => step.IsDryRunOnly),
            Steps = steps,
        };
    }

    private static LongLiveMapOverviewShellAllocationTarget? FindAllocationTarget(LongLiveMapOverviewShellAllocationPlan plan, string pageId)
    {
        foreach (var target in plan.Targets)
        {
            if (string.Equals(target.PageId, pageId, StringComparison.Ordinal))
            {
                return target;
            }
        }

        return null;
    }

    private static int CountByAction(IReadOnlyList<LongLiveMapOverviewExecutionStep> steps, Func<LongLiveMapOverviewExecutionStep, bool> predicate)
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
