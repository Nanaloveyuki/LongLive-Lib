using System;
using BepInEx.Logging;
using LongLive.Mods.Maps;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveSceneRoutingCoordinator
{
    private readonly ManualLogSource _logger;
    private readonly ILongLiveSceneRoutingService _sceneRouting;
    private readonly LongLiveSceneRoutingFeatureCollection _features;

    public LongLiveSceneRoutingCoordinator(
        ManualLogSource logger,
        ILongLiveSceneRoutingService sceneRouting,
        LongLiveSceneRoutingFeatureCollection features)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sceneRouting = sceneRouting ?? throw new ArgumentNullException(nameof(sceneRouting));
        _features = features ?? throw new ArgumentNullException(nameof(features));
    }

    public void InitializeFeatures()
    {
        _features.InitializeAll(_sceneRouting);
    }

    public void RegisterSource(ILongLiveSceneRouteRegistrationSource source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var plan = source.CreatePlan();
        RegisterPlan(plan, source.Name);
    }

    public void RegisterPlan(LongLiveMapRegistryPlan plan, string sourceName)
    {
        if (plan is null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        if (!plan.Validation.IsValid)
        {
            foreach (var issue in plan.Validation.Issues)
            {
                if (issue.IsError)
                {
                    _logger.LogWarning($"LongLive scene routing skipped invalid plan issue from {sourceName}: {issue.Code}: {issue.Message}");
                }
            }

            return;
        }

        _sceneRouting.RegisterPlan(plan);
        _features.RegisterPlanAcrossFeatures(plan);
        _logger.LogInfo($"LongLive scene routing coordinator registered plan from source={sourceName}, scenes={plan.Draft.Scenes.Count}, pages={plan.Draft.Pages.Count}, nodes={plan.Draft.Nodes.Count}");
    }
}
