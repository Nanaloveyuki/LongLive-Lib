using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using LongLive.Mods.Compatibility;
using LongLive.Mods.Maps;
using LongLive.Mods.SceneRouting;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public static class LongLivePluginContext
{
    private static ILongLiveSceneRoutingService? _sceneRouting;
    private static LongLiveSceneRoutingHost? _sceneRoutingHost;
    private static LongLiveCompatibilityRuntime? _compatibility;

    public static bool IsInitialized => LongLivePlugin.Instance is not null;

    public static ILongLiveSceneRoutingService SceneRouting
    {
        get
        {
            if (_sceneRouting is null)
            {
                _sceneRouting = new LongLiveBepInExSceneRoutingService(GetLogger());
            }

            return _sceneRouting;
        }
    }

    public static LongLiveSceneRoutingHost SceneRoutingHost
    {
        get
        {
            if (_sceneRoutingHost is null)
            {
                _sceneRoutingHost = new LongLiveSceneRoutingHost(GetLogger(), SceneRouting);
            }

            return _sceneRoutingHost;
        }
    }

    public static ILongLiveMapOverviewFeature MapOverview => SceneRoutingHost.MapOverview;

    public static ILongLiveCustomMapRuntimeFeature CustomMapRuntime => SceneRoutingHost.CustomMapRuntime;

    public static LongLiveCompatibilityRuntime Compatibility
    {
        get
        {
            if (_compatibility is null)
            {
                _compatibility = new LongLiveCompatibilityRuntime(GetLogger());
            }

            return _compatibility;
        }
    }

    public static LongLiveCompatibilitySnapshot GetCompatibilitySnapshot()
    {
        Compatibility.RefreshDynamicState();
        return Compatibility.CaptureSnapshot();
    }

    public static void RegisterSceneRouteSource(ILongLiveSceneRouteRegistrationSource source)
    {
        SceneRoutingHost.RegisterSource(source);
    }

    public static void RegisterSceneRoutingFeature(ILongLiveSceneRoutingFeature feature)
    {
        SceneRoutingHost.RegisterFeature(feature);
    }

    public static void RegisterMapRegistryPlan(LongLiveMapRegistryPlan plan, string sourceName = "external")
    {
        SceneRoutingHost.RegisterPlan(plan, sourceName);
    }

    public static void RegisterSceneLocalTopologyBatch(LongLiveSceneLocalTopologyBatch batch)
    {
        if (batch is null)
        {
            throw new ArgumentNullException(nameof(batch));
        }

        if (CustomMapRuntime is LongLiveCustomMapRuntimeFeatureShell shell)
        {
            shell.RegisterSceneLocalTopologyBatch(batch);
            return;
        }

        throw new InvalidOperationException("The current custom map runtime feature does not support scene-local topology registration.");
    }

    public static LongLiveMapRegistryPlan CreateMapRegistryPlan(LongLiveMapRegistryDraft draft, LongLiveMapHostAllocationRanges? ranges = null)
    {
        if (draft is null)
        {
            throw new ArgumentNullException(nameof(draft));
        }

        return new LongLiveMapRegistryPlanner().CreatePlan(draft, ranges);
    }

    public static LongLiveMapRegistryPlan RegisterMapRegistryDraft(
        LongLiveMapRegistryDraft draft,
        string sourceName = "external",
        LongLiveMapHostAllocationRanges? ranges = null)
    {
        if (draft is null)
        {
            throw new ArgumentNullException(nameof(draft));
        }

        var plan = CreateMapRegistryPlan(draft, ranges);
        RegisterMapRegistryPlan(plan, sourceName);
        return plan;
    }

    public static bool TryGetSceneRoutingFeature<TFeature>(out TFeature? feature)
        where TFeature : class, ILongLiveSceneRoutingFeature
    {
        return SceneRoutingHost.Features.TryGet(out feature);
    }

    public static NextRuntimeFacade GetRuntime()
    {
        var plugin = LongLivePlugin.Instance;
        if (plugin is null)
        {
            throw new InvalidOperationException("LongLivePlugin has not been initialized yet.");
        }

        return plugin.Runtime;
    }

    public static ManualLogSource GetLogger()
    {
        var logger = LongLivePlugin.LogSource;
        if (logger is null)
        {
            throw new InvalidOperationException("LongLivePlugin logger is not available yet.");
        }

        return logger;
    }

    public static LongLiveHostOptions GetOptions()
    {
        var plugin = LongLivePlugin.Instance;
        if (plugin is null)
        {
            throw new InvalidOperationException("LongLivePlugin has not been initialized yet.");
        }

        return plugin.Options;
    }

    public static bool TryGetHostHandshake(out LongLiveHostHandshake? handshake)
    {
        var plugin = LongLivePlugin.Instance;
        if (plugin is null)
        {
            handshake = null;
            return false;
        }

        handshake = plugin.RefreshHandshake();
        return true;
    }

    public static LongLiveHostHandshake GetHostHandshake()
    {
        var plugin = LongLivePlugin.Instance;
        if (plugin is null)
        {
            throw new InvalidOperationException("LongLivePlugin has not been initialized yet.");
        }

        return plugin.RefreshHandshake();
    }

    public static bool HasCapability(string capability)
    {
        return TryGetHostHandshake(out var handshake) && handshake is not null && handshake.Supports(capability);
    }

    public static IReadOnlyCollection<string> GetCapabilities()
    {
        return GetHostHandshake().Capabilities;
    }

    public static LongLiveSceneLocalTopologyRuntimeSnapshot GetSceneLocalTopologyRuntimeSnapshot(int sampleLimit = 8)
    {
        return LongLiveSceneLocalTopologyRuntime.CaptureSnapshot(sampleLimit);
    }
}
