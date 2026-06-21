using System;
using System.Collections.Generic;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapOverviewShellAllocationPlanBuilder
{
    public static LongLiveMapOverviewShellAllocationPlan Build(LongLiveMapOverviewInstallPlan installPlan, LongLiveMapOverviewHostBindingRuntimeSnapshot bindingRuntime)
    {
        if (installPlan is null)
        {
            throw new ArgumentNullException(nameof(installPlan));
        }

        if (bindingRuntime is null)
        {
            throw new ArgumentNullException(nameof(bindingRuntime));
        }

        var targets = new List<LongLiveMapOverviewShellAllocationTarget>();
        foreach (var pageTarget in installPlan.PageTargets)
        {
            var bindingTarget = FindBindingTarget(bindingRuntime, pageTarget.PageId);
            var hostSurface = bindingTarget?.ExpectsSeaHost == true ? "Sea" : "NingZhou";
            var canReuseExistingShell = !pageTarget.RequiresHostInjection;
            var requiresDedicatedShell = pageTarget.RequiresHostInjection;
            var canBindInCurrentSession = !pageTarget.RequiresHostInjection || (bindingTarget?.HasExpectedHostRoot == true && bindingTarget.HasInjectionAnchor);
            var allocationReason = ResolveAllocationReason(pageTarget, bindingTarget, canReuseExistingShell, canBindInCurrentSession);

            targets.Add(new LongLiveMapOverviewShellAllocationTarget
            {
                PageId = pageTarget.PageId,
                OwningModId = pageTarget.OwningModId,
                DisplayName = pageTarget.DisplayName,
                ShellKind = canReuseExistingShell ? "reuse-host-shell" : "dedicated-host-shell",
                HostSurface = hostSurface,
                RequiresHostInjection = pageTarget.RequiresHostInjection,
                CanReuseExistingHostShell = canReuseExistingShell,
                RequiresDedicatedShell = requiresDedicatedShell,
                CanBindInCurrentSession = canBindInCurrentSession,
                AllocationReason = allocationReason,
                AnchorName = bindingTarget?.InjectionAnchorName ?? string.Empty,
                ProjectionCount = pageTarget.ProjectionCount,
                RegionCount = pageTarget.RegionCount,
                NodeCount = pageTarget.NodeCount,
            });
        }

        return new LongLiveMapOverviewShellAllocationPlan
        {
            TargetCount = targets.Count,
            ReuseExistingShellCount = CountBy(targets, static target => target.CanReuseExistingHostShell),
            DedicatedShellCount = CountBy(targets, static target => target.RequiresDedicatedShell),
            BindableTargetCount = CountBy(targets, static target => target.CanBindInCurrentSession),
            Targets = targets,
        };
    }

    private static LongLiveMapOverviewHostBindingTarget? FindBindingTarget(LongLiveMapOverviewHostBindingRuntimeSnapshot snapshot, string pageId)
    {
        foreach (var target in snapshot.Targets)
        {
            if (string.Equals(target.PageId, pageId, StringComparison.Ordinal))
            {
                return target;
            }
        }

        return null;
    }

    private static string ResolveAllocationReason(LongLiveMapOverviewPageInstallTarget pageTarget, LongLiveMapOverviewHostBindingTarget? bindingTarget, bool canReuseExistingShell, bool canBindInCurrentSession)
    {
        if (canReuseExistingShell)
        {
            return "base-game-shaped page target can continue using the current host shell.";
        }

        if (bindingTarget is null)
        {
            return "external page target requires a dedicated shell, but current session has not produced a host binding sample yet.";
        }

        if (!bindingTarget.HasExpectedHostRoot)
        {
            return "external page target requires a dedicated shell and the expected host root is currently unavailable.";
        }

        if (!bindingTarget.HasInjectionAnchor)
        {
            return "external page target requires a dedicated shell, but the expected host anchor is currently unavailable.";
        }

        return canBindInCurrentSession
            ? "external page target requires a dedicated shell and current session already exposes a usable host root and anchor."
            : "external page target requires a dedicated shell, but current session still cannot bind it safely.";
    }

    private static int CountBy(IReadOnlyList<LongLiveMapOverviewShellAllocationTarget> targets, Func<LongLiveMapOverviewShellAllocationTarget, bool> predicate)
    {
        var count = 0;
        foreach (var target in targets)
        {
            if (predicate(target))
            {
                count++;
            }
        }

        return count;
    }
}
