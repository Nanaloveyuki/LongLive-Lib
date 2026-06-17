using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using LongLive.Mods;
using LongLive.Mods.Installation;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

public sealed class LongLiveJsonModDemoInstaller : ILongLiveInstaller
{
    private readonly ManualLogSource _logger;
    private readonly NextRuntimeFacade _runtime;
    private readonly LongLiveHostOptions _options;
    private readonly LongLiveModToolkit _toolkit;
    private readonly LongLiveContentRegistryProvider _contentRegistryProvider;
    private const string DefaultLocalTestGroupName = "LongLive.LocalTest";
    private const string DefaultLocalTestModName = "modLongLiveDemo";
    private const string DefaultJsonPackageFolderName = "json-mod-demo";

    public LongLiveJsonModDemoInstaller(
        ManualLogSource logger,
        NextRuntimeFacade runtime,
        LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _toolkit = new LongLiveModToolkit();
        _contentRegistryProvider = new LongLiveContentRegistryProvider(_logger, _runtime, _options);
    }

    public string Name => "LongLiveJsonModDemoInstaller";

    public void Install()
    {
        if (!_options.EnableJsonModDemoInstall.Value)
        {
            _logger.LogInfo("JSON mod demo install is disabled.");
            return;
        }

        var demoPath = ResolveDemoPath();
        if (string.IsNullOrWhiteSpace(demoPath))
        {
            _logger.LogWarning("JSON mod demo install is enabled but no demo package path could be resolved. Skipping JSON mod demo install.");
            return;
        }

        var fullDemoPath = Path.GetFullPath(demoPath);
        if (!Directory.Exists(fullDemoPath))
        {
            _logger.LogWarning($"JSON mod demo directory does not exist: {fullDemoPath}");
            return;
        }

        LongLiveModLoadReport loadReport;
        try
        {
            loadReport = _toolkit.LoadAndValidate(fullDemoPath);
        }
        catch (Exception exception)
        {
            _logger.LogError($"Failed to load JSON mod demo package from {fullDemoPath}: {exception}");
            return;
        }

        _logger.LogInfo($"Loaded JSON mod demo package: {loadReport.Package.Manifest.Id} {loadReport.Package.Manifest.Version} from {fullDemoPath}");

        foreach (var issue in loadReport.Validation.Issues)
        {
            if (issue.Severity == LongLive.Mods.Validation.LongLiveModValidationSeverity.Warning)
            {
                _logger.LogWarning($"JSON mod validation warning {issue.Code}: {issue.Message}");
            }
        }

        if (!loadReport.Validation.IsValid)
        {
            foreach (var issue in loadReport.Validation.Issues)
            {
                if (issue.Severity == LongLive.Mods.Validation.LongLiveModValidationSeverity.Error)
                {
                    _logger.LogError($"JSON mod validation error {issue.Code}: {issue.Message}");
                }
            }

            _logger.LogWarning("JSON mod demo package is not valid. Skipping install.");
            return;
        }

        var contentRegistry = _contentRegistryProvider.CreateRegistry();
        var installer = new LongLiveModInstaller(
            _runtime.CommandRegistry,
            _runtime.QueryRegistry,
            new LongLiveBuiltinCapabilityRegistry(_runtime.StateStore),
            contentRegistry);

        var installReport = installer.Install(loadReport.Package);
        foreach (var skipped in installReport.SkippedEntries)
        {
            _logger.LogInfo(skipped);
        }

        _logger.LogInfo($"JSON mod demo installed commands: {installReport.InstalledCommands.Count}");
        _logger.LogInfo($"JSON mod demo installed queries: {installReport.InstalledQueries.Count}");

        if (installReport.SkippedEntries.Count > 0)
        {
            _logger.LogInfo($"JSON mod demo skipped entries: {installReport.SkippedEntries.Count}");
        }

        var installedContentCount = installReport.ContentEntries.Count(entry => entry.Status == LongLiveContentInstallStatus.Installed);
        var deferredContentCount = installReport.ContentEntries.Count(entry => entry.Status == LongLiveContentInstallStatus.Deferred);
        var skippedContentCount = installReport.ContentEntries.Count(entry => entry.Status == LongLiveContentInstallStatus.Skipped);

        _logger.LogInfo($"JSON mod demo content install summary: installed={installedContentCount}, deferred={deferredContentCount}, skipped={skippedContentCount}");

        foreach (var contentEntry in installReport.ContentEntries)
        {
            if (contentEntry.Status != LongLiveContentInstallStatus.Installed)
            {
                _logger.LogInfo($"JSON mod content {contentEntry.ContentType} {contentEntry.ContentId}: {contentEntry.Status} [{contentEntry.ReasonCode}] - {contentEntry.Message}");
            }
        }

        LogContentDryRunSummary(installReport.ContentEntries);

        _logger.LogInfo($"JSON mod demo content summary: items={loadReport.Package.Items?.Items.Count ?? 0}, skills={loadReport.Package.Skills?.Skills.Count ?? 0}, buffs={loadReport.Package.Buffs?.Buffs.Count ?? 0}, assets={loadReport.Package.Assets?.Assets.Count ?? 0}");
    }

    private string ResolveDemoPath()
    {
        var configuredPath = _options.JsonModDemoPath.Value;
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var report = _runtime.ContentInspector.Inspect();
        if (!report.Capabilities.CanResolveLocalModsDirectory || string.IsNullOrWhiteSpace(report.LocalModsDirectory))
        {
            return string.Empty;
        }

        var defaultPath = Path.Combine(
            report.LocalModsDirectory,
            DefaultLocalTestGroupName,
            "plugins",
            "Next",
            DefaultLocalTestModName,
            "LongLive",
            DefaultJsonPackageFolderName);

        if (Directory.Exists(defaultPath))
        {
            _logger.LogInfo($"JsonModDemoPath is empty. Using default local-test demo package path: {defaultPath}");
            return defaultPath;
        }

        return string.Empty;
    }

    private void LogContentDryRunSummary(IReadOnlyList<LongLiveContentInstallEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        foreach (var group in entries
                     .GroupBy(entry => new { entry.ContentType, entry.Status, entry.ReasonCode })
                     .OrderBy(group => group.Key.ContentType, StringComparer.Ordinal)
                     .ThenBy(group => group.Key.Status)
                     .ThenBy(group => group.Key.ReasonCode, StringComparer.Ordinal))
        {
            _logger.LogInfo($"JSON mod dry-run group: type={group.Key.ContentType}, status={group.Key.Status}, reason={group.Key.ReasonCode}, count={group.Count()}");
        }
    }
}
