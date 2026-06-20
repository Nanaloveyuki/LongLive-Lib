using System;
using System.Collections.Generic;
using BepInEx.Logging;
using LongLive.Mods.Maps;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveThirdPartyMapDraftRegistrationService
{
    private static readonly HashSet<string> RegisteredAdapters = new HashSet<string>(StringComparer.Ordinal);
    private static readonly HashSet<string> RegisteredSceneLocalTopologySources = new HashSet<string>(StringComparer.Ordinal);
    private readonly ManualLogSource _logger;

    public LongLiveThirdPartyMapDraftRegistrationService(ManualLogSource logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public LongLiveMapRegistryPlan? TryRegister(ILongLiveThirdPartyMapDraftAdapter adapter)
    {
        if (adapter is null)
        {
            throw new ArgumentNullException(nameof(adapter));
        }

        if (RegisteredAdapters.Contains(adapter.AdapterId))
        {
            return null;
        }

        if (!adapter.CanBuildDraft())
        {
            _logger.LogInfo($"LongLive map draft adapter skipped: adapter={adapter.AdapterId}, sourceMod={adapter.SourceModId}");
            return null;
        }

        var draft = adapter.BuildDraft();
        var sourceName = $"adapter:{adapter.AdapterId}";
        var plan = LongLivePluginContext.RegisterMapRegistryDraft(draft, sourceName);
        RegisteredAdapters.Add(adapter.AdapterId);
        _logger.LogInfo($"LongLive map draft adapter registered: adapter={adapter.AdapterId}, sourceMod={adapter.SourceModId}, scenes={plan.Draft.Scenes.Count}, pages={plan.Draft.Pages.Count}, nodes={plan.Draft.Nodes.Count}");

        if (adapter is LongLiveJToolsMapDraftAdapter)
        {
            TryRegisterJToolsSceneLocalTopology();
        }

        return plan;
    }

    public void TryRegisterPending(IReadOnlyList<ILongLiveThirdPartyMapDraftAdapter> adapters)
    {
        if (adapters is null)
        {
            throw new ArgumentNullException(nameof(adapters));
        }

        foreach (var adapter in adapters)
        {
            if (RegisteredAdapters.Contains(adapter.AdapterId))
            {
                continue;
            }

            TryRegister(adapter);
        }
    }

    private void TryRegisterJToolsSceneLocalTopology()
    {
        const string sourceId = "tierneyjohn.jtools.scene-local-topology";

        if (RegisteredSceneLocalTopologySources.Contains(sourceId))
        {
            return;
        }

        if (!LongLiveJToolsMapDraftAdapter.CanBuildSceneLocalTopologyBatch())
        {
            _logger.LogInfo("LongLive scene-local topology registration deferred for JTools because MapInfos is still empty.");
            return;
        }

        try
        {
            var batch = LongLiveJToolsMapDraftAdapter.BuildSceneLocalTopologyBatch();
            if (batch.Topologies.Count == 0)
            {
                _logger.LogInfo("LongLive scene-local topology registration deferred for JTools because the built batch was empty.");
                return;
            }

            LongLivePluginContext.RegisterSceneLocalTopologyBatch(batch);
            RegisteredSceneLocalTopologySources.Add(sourceId);
            _logger.LogInfo($"LongLive scene-local topology registered: sourceMod=tierneyjohn.jtools, topologies={batch.Topologies.Count}, nodes={batch.Nodes.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"LongLive scene-local topology registration skipped for JTools: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
