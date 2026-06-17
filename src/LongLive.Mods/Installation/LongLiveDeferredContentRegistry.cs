using System;
using LongLive.Mods.Models;

namespace LongLive.Mods.Installation;

public sealed class LongLiveDeferredContentRegistry : ILongLiveContentRegistry
{
    public LongLiveContentInstallEntry InstallItem(LongLiveContentInstallRequest<LongLiveItemDefinition> request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return CreateDeferredEntry(
            "item",
            request.Content.Id.ToString(),
            $"Item runtime injection is not installed yet for mod {request.Context.Manifest.Id}.");
    }

    public LongLiveContentInstallEntry InstallSkill(LongLiveContentInstallRequest<LongLiveSkillDefinition> request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return CreateDeferredEntry(
            "skill",
            request.Content.Id.ToString(),
            $"Skill runtime injection is not installed yet for mod {request.Context.Manifest.Id}.");
    }

    public LongLiveContentInstallEntry InstallBuff(LongLiveContentInstallRequest<LongLiveBuffDefinition> request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return CreateDeferredEntry(
            "buff",
            request.Content.Id.ToString(),
            $"Buff runtime injection is not installed yet for mod {request.Context.Manifest.Id}.");
    }

    public LongLiveContentInstallEntry InstallAsset(LongLiveContentInstallRequest<LongLiveAssetMappingDefinition> request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return CreateDeferredEntry(
            "asset",
            request.Content.Id,
            $"Asset mapping installation is not installed yet for mod {request.Context.Manifest.Id}. Root={request.Context.RootDirectory}");
    }

    private static LongLiveContentInstallEntry CreateDeferredEntry(string contentType, string contentId, string message)
    {
        return new LongLiveContentInstallEntry(contentType, contentId, LongLiveContentInstallStatus.Deferred, message);
    }
}
