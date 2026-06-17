using System;
using BepInEx.Logging;
using LongLive.Mods.Installation;
using LongLive.Mods.Models;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveNextContentRegistry : ILongLiveContentRegistry
{
    private readonly ManualLogSource _logger;
    private readonly NextRuntimeFacade _runtime;

    public LongLiveNextContentRegistry(ManualLogSource logger, NextRuntimeFacade runtime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public LongLiveContentInstallEntry InstallItem(LongLiveContentInstallRequest<LongLiveItemDefinition> request)
    {
        return CreateDeferredEntry(
            request,
            "item",
            request.Content.Id.ToString(),
            "Next content backend shell is selected, but item runtime injection is not implemented yet.");
    }

    public LongLiveContentInstallEntry InstallSkill(LongLiveContentInstallRequest<LongLiveSkillDefinition> request)
    {
        return CreateDeferredEntry(
            request,
            "skill",
            request.Content.Id.ToString(),
            "Next content backend shell is selected, but skill runtime injection is not implemented yet.");
    }

    public LongLiveContentInstallEntry InstallBuff(LongLiveContentInstallRequest<LongLiveBuffDefinition> request)
    {
        return CreateDeferredEntry(
            request,
            "buff",
            request.Content.Id.ToString(),
            "Next content backend shell is selected, but buff runtime injection is not implemented yet.");
    }

    public LongLiveContentInstallEntry InstallAsset(LongLiveContentInstallRequest<LongLiveAssetMappingDefinition> request)
    {
        return CreateDeferredEntry(
            request,
            "asset",
            request.Content.Id,
            "Next content backend shell is selected, but asset runtime injection is not implemented yet.");
    }

    private LongLiveContentInstallEntry CreateDeferredEntry<TContent>(
        LongLiveContentInstallRequest<TContent> request,
        string contentType,
        string contentId,
        string message)
        where TContent : class
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogDebug($"Deferred Next content backend request for {contentType} {contentId} in mod {request.Context.Manifest.Id}. RuntimeAvailable={_runtime.IsAvailable}");
        return new LongLiveContentInstallEntry(contentType, contentId, LongLiveContentInstallStatus.Deferred, message);
    }
}
