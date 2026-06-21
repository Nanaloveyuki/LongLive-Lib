using System;
using System.Collections.Generic;
using System.IO;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveHostHandshakeFactory
{
    public const int CurrentHandshakeVersion = 1;

    public static LongLiveHostHandshake Create(LongLivePlugin plugin)
    {
        var capabilities = BuildCapabilities(plugin);
        var installRoot = Path.GetDirectoryName(typeof(LongLivePlugin).Assembly.Location) ?? string.Empty;

        return new LongLiveHostHandshake(
            LongLivePluginMetadata.PluginGuid,
            LongLivePluginMetadata.PluginName,
            LongLivePluginMetadata.PluginVersion,
            CurrentHandshakeVersion,
            plugin.Runtime.IsAvailable,
            installRoot,
            capabilities,
            DateTimeOffset.UtcNow);
    }

    private static IReadOnlyCollection<string> BuildCapabilities(LongLivePlugin plugin)
    {
        var options = plugin.Options;
        var capabilities = new List<string>
        {
            LongLiveHostCapability.HostBootstrap,
            LongLiveHostCapability.MainMenuEntry,
            LongLiveHostCapability.MapRegistryPlanning,
            LongLiveHostCapability.SceneRouting,
            LongLiveHostCapability.MapOverview,
            LongLiveHostCapability.CustomMapRuntime,
            LongLiveHostCapability.SceneLocalTopology,
            LongLiveHostCapability.CompatibilityRedirect,
        };

        if (plugin.Runtime.IsAvailable)
        {
            capabilities.Add(LongLiveHostCapability.NextRuntime);
        }

        if (options.EnableNativeProbe.Value)
        {
            capabilities.Add(LongLiveHostCapability.NativeProbe);
        }

        if (options.EnableBattleTrace.Value)
        {
            capabilities.Add(LongLiveHostCapability.BattleTrace);
        }

        if (options.EnableExperimentalBattleGuard.Value)
        {
            capabilities.Add(LongLiveHostCapability.BattleGuard);
        }

        if (options.EnableBulkItemUseOptimization.Value)
        {
            capabilities.Add(LongLiveHostCapability.BulkItemUse);
        }

        if (options.EnableMapTrace.Value)
        {
            capabilities.Add(LongLiveHostCapability.MapTrace);
        }

        if (options.EnableAutoExportMapSnapshot.Value)
        {
            capabilities.Add(LongLiveHostCapability.MapSnapshotExport);
        }

        if (options.EnableAutoExportSceneRoutingPlanningDump.Value)
        {
            capabilities.Add(LongLiveHostCapability.SceneRoutingPlanningDump);
        }

        return capabilities.AsReadOnly();
    }
}
