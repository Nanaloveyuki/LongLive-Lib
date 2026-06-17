using System;
using System.IO;
using BepInEx.Logging;
using LongLive.Mods.Installation;
using LongLive.Mods.Models;
using LongLive.Next.Runtime;
using LongLive.Next.Runtime.Inspection;

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
        var preflight = RunDataPreflight(request, "item", request.Content.Id.ToString());
        if (!preflight.CanContinue)
        {
            return new LongLiveContentInstallEntry("item", request.Content.Id.ToString(), LongLiveContentInstallStatus.Skipped, preflight.ReasonCode, preflight.Message);
        }

        return CreateDeferredEntry(
            request,
            "item",
            request.Content.Id.ToString(),
            preflight.ReasonCode,
            preflight.Message);
    }

    public LongLiveContentInstallEntry InstallSkill(LongLiveContentInstallRequest<LongLiveSkillDefinition> request)
    {
        var preflight = RunDataPreflight(request, "skill", request.Content.Id.ToString());
        if (!preflight.CanContinue)
        {
            return new LongLiveContentInstallEntry("skill", request.Content.Id.ToString(), LongLiveContentInstallStatus.Skipped, preflight.ReasonCode, preflight.Message);
        }

        return CreateDeferredEntry(
            request,
            "skill",
            request.Content.Id.ToString(),
            preflight.ReasonCode,
            preflight.Message);
    }

    public LongLiveContentInstallEntry InstallBuff(LongLiveContentInstallRequest<LongLiveBuffDefinition> request)
    {
        var preflight = RunDataPreflight(request, "buff", request.Content.Id.ToString());
        if (!preflight.CanContinue)
        {
            return new LongLiveContentInstallEntry("buff", request.Content.Id.ToString(), LongLiveContentInstallStatus.Skipped, preflight.ReasonCode, preflight.Message);
        }

        return CreateDeferredEntry(
            request,
            "buff",
            request.Content.Id.ToString(),
            preflight.ReasonCode,
            preflight.Message);
    }

    public LongLiveContentInstallEntry InstallAsset(LongLiveContentInstallRequest<LongLiveAssetMappingDefinition> request)
    {
        var preflight = RunAssetPreflight(request);
        if (!preflight.CanContinue)
        {
            return new LongLiveContentInstallEntry("asset", request.Content.Id, LongLiveContentInstallStatus.Skipped, preflight.ReasonCode, preflight.Message);
        }

        return CreateDeferredEntry(
            request,
            "asset",
            request.Content.Id,
            preflight.ReasonCode,
            preflight.Message);
    }

    private LongLiveContentInstallEntry CreateDeferredEntry<TContent>(
        LongLiveContentInstallRequest<TContent> request,
        string contentType,
        string contentId,
        string reasonCode,
        string message)
        where TContent : class
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogDebug($"Deferred Next content backend request for {contentType} {contentId} in mod {request.Context.Manifest.Id}. RuntimeAvailable={_runtime.IsAvailable}");
        return new LongLiveContentInstallEntry(contentType, contentId, LongLiveContentInstallStatus.Deferred, reasonCode, message);
    }

    private LongLiveNextContentPreflightResult RunDataPreflight<TContent>(LongLiveContentInstallRequest<TContent> request, string contentType, string contentId)
        where TContent : class
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var report = _runtime.ContentInspector.Inspect();
        if (!report.IsAvailable)
        {
            return new LongLiveNextContentPreflightResult(false, LongLiveContentInstallReasonCode.NextRuntimeUnavailable, "Next runtime is not available for content backend preflight.");
        }

        if (!report.Capabilities.HasModManager || !report.Capabilities.HasJsonDataPatch)
        {
            return new LongLiveNextContentPreflightResult(false, LongLiveContentInstallReasonCode.NextLifecycleUnavailable, "Next mod lifecycle entry points are not fully available for content backend preflight.");
        }

        var message = $"Next content backend preflight passed for {contentType} {contentId} in mod {request.Context.Manifest.Id}, but runtime injection is not implemented yet.";
        _logger.LogDebug(message);
        return new LongLiveNextContentPreflightResult(true, LongLiveContentInstallReasonCode.NextPreflightDeferred, message);
    }

    private LongLiveNextContentPreflightResult RunAssetPreflight(LongLiveContentInstallRequest<LongLiveAssetMappingDefinition> request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var report = _runtime.ContentInspector.Inspect();
        if (!report.IsAvailable)
        {
            return new LongLiveNextContentPreflightResult(false, LongLiveContentInstallReasonCode.NextRuntimeUnavailable, "Next runtime is not available for asset backend preflight.");
        }

        if (!report.Capabilities.HasResourcesManager)
        {
            return new LongLiveNextContentPreflightResult(false, LongLiveContentInstallReasonCode.NextResourcesUnavailable, "Next ResourcesManager is not available for asset backend preflight.");
        }

        var fullSourcePath = Path.GetFullPath(Path.Combine(request.Context.RootDirectory, request.Content.Source));
        if (!File.Exists(fullSourcePath))
        {
            return new LongLiveNextContentPreflightResult(false, LongLiveContentInstallReasonCode.AssetSourceMissing, $"Asset source file does not exist: {fullSourcePath}");
        }

        var supportsResourcePatch = report.Capabilities.HasModResourcesSpritePatch || report.Capabilities.HasModResourcesTexturePatch;
        if (!supportsResourcePatch)
        {
            return new LongLiveNextContentPreflightResult(false, LongLiveContentInstallReasonCode.NextResourcePatchUnavailable, "Next resource patch entry points are not available for asset backend preflight.");
        }

        var message = $"Next asset backend preflight passed for {request.Content.Id} -> {fullSourcePath}, but runtime injection is not implemented yet.";
        _logger.LogDebug(message);
        return new LongLiveNextContentPreflightResult(true, LongLiveContentInstallReasonCode.NextPreflightDeferred, message);
    }
}
