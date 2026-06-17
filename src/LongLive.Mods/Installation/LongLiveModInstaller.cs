using System;
using LongLive.Mods.Models;
using LongLive.Next.Abstractions.Commands;
using LongLive.Next.Abstractions.Queries;
using LongLive.Next.Abstractions.State;

namespace LongLive.Mods.Installation;

public sealed class LongLiveModInstaller
{
    private readonly INextCommandRegistry _commandRegistry;
    private readonly INextQueryRegistry _queryRegistry;
    private readonly ILongLiveModCapabilityRegistry _capabilities;
    private readonly ILongLiveContentRegistry _contentRegistry;

    public LongLiveModInstaller(
        INextCommandRegistry commandRegistry,
        INextQueryRegistry queryRegistry,
        INextStateStore stateStore)
        : this(
            commandRegistry,
            queryRegistry,
            new LongLiveBuiltinCapabilityRegistry(stateStore),
            new LongLiveDeferredContentRegistry())
    {
    }

    public LongLiveModInstaller(
        INextCommandRegistry commandRegistry,
        INextQueryRegistry queryRegistry,
        ILongLiveModCapabilityRegistry capabilities)
        : this(commandRegistry, queryRegistry, capabilities, new LongLiveDeferredContentRegistry())
    {
    }

    public LongLiveModInstaller(
        INextCommandRegistry commandRegistry,
        INextQueryRegistry queryRegistry,
        ILongLiveModCapabilityRegistry capabilities,
        ILongLiveContentRegistry contentRegistry)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _queryRegistry = queryRegistry ?? throw new ArgumentNullException(nameof(queryRegistry));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        _contentRegistry = contentRegistry ?? throw new ArgumentNullException(nameof(contentRegistry));
    }

    public LongLiveInstalledModReport Install(LongLiveModPackage package)
    {
        if (package is null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        var contentContext = new LongLiveContentInstallContext(package);
        var report = new LongLiveInstalledModReport();

        InstallCommands(package.Commands, report);
        InstallQueries(package.Queries, report);
        InstallItems(contentContext, package.Items, report);
        InstallSkills(contentContext, package.Skills, report);
        InstallBuffs(contentContext, package.Buffs, report);
        InstallAssets(contentContext, package.Assets, report);

        return report;
    }

    private void InstallCommands(LongLiveCommandFile? commandFile, LongLiveInstalledModReport report)
    {
        if (commandFile is null)
        {
            return;
        }

        foreach (var command in commandFile.Commands)
        {
            if (!string.Equals(command.Backend, "builtin", StringComparison.OrdinalIgnoreCase))
            {
                report.AddSkippedEntry($"Skipped command {command.Id}: backend {command.Backend} is not installed yet.");
                continue;
            }

            if (!_capabilities.TryGetCommandHandler(command.Handler, out var handler))
            {
                report.AddSkippedEntry($"Skipped command {command.Id}: builtin handler {command.Handler} is not registered.");
                continue;
            }

            _commandRegistry.Register(command.Id, handler);
            report.AddInstalledCommand(command.Id);
        }
    }

    private void InstallQueries(LongLiveQueryFile? queryFile, LongLiveInstalledModReport report)
    {
        if (queryFile is null)
        {
            return;
        }

        foreach (var query in queryFile.Queries)
        {
            if (!string.Equals(query.Backend, "builtin", StringComparison.OrdinalIgnoreCase))
            {
                report.AddSkippedEntry($"Skipped query {query.Id}: backend {query.Backend} is not installed yet.");
                continue;
            }

            if (!_capabilities.TryGetQueryHandler(query.Handler, out var handler))
            {
                report.AddSkippedEntry($"Skipped query {query.Id}: builtin handler {query.Handler} is not registered.");
                continue;
            }

            _queryRegistry.Register(query.Id, handler);
            report.AddInstalledQuery(query.Id);
        }
    }

    private void InstallItems(LongLiveContentInstallContext context, LongLiveItemFile? itemFile, LongLiveInstalledModReport report)
    {
        if (itemFile is null)
        {
            return;
        }

        foreach (var item in itemFile.Items)
        {
            report.AddContentEntry(_contentRegistry.InstallItem(new LongLiveContentInstallRequest<LongLiveItemDefinition>(context, item)));
        }
    }

    private void InstallSkills(LongLiveContentInstallContext context, LongLiveSkillFile? skillFile, LongLiveInstalledModReport report)
    {
        if (skillFile is null)
        {
            return;
        }

        foreach (var skill in skillFile.Skills)
        {
            report.AddContentEntry(_contentRegistry.InstallSkill(new LongLiveContentInstallRequest<LongLiveSkillDefinition>(context, skill)));
        }
    }

    private void InstallBuffs(LongLiveContentInstallContext context, LongLiveBuffFile? buffFile, LongLiveInstalledModReport report)
    {
        if (buffFile is null)
        {
            return;
        }

        foreach (var buff in buffFile.Buffs)
        {
            report.AddContentEntry(_contentRegistry.InstallBuff(new LongLiveContentInstallRequest<LongLiveBuffDefinition>(context, buff)));
        }
    }

    private void InstallAssets(LongLiveContentInstallContext context, LongLiveAssetMappingFile? assetFile, LongLiveInstalledModReport report)
    {
        if (assetFile is null)
        {
            return;
        }

        foreach (var asset in assetFile.Assets)
        {
            report.AddContentEntry(_contentRegistry.InstallAsset(new LongLiveContentInstallRequest<LongLiveAssetMappingDefinition>(context, asset)));
        }
    }
}
