using System.Collections.Generic;

namespace LongLive.Mods.Models;

public sealed class LongLiveModPackage
{
    public LongLiveModPackage(
        string rootDirectory,
        LongLiveModManifest manifest,
        LongLiveStateKeyFile? stateKeys,
        LongLiveCommandFile? commands,
        LongLiveQueryFile? queries,
        LongLiveItemFile? items,
        LongLiveSkillFile? skills,
        LongLiveBuffFile? buffs,
        LongLiveAssetMappingFile? assets,
        IReadOnlyList<LongLiveLocaleResource> locales)
    {
        RootDirectory = rootDirectory;
        Manifest = manifest;
        StateKeys = stateKeys;
        Commands = commands;
        Queries = queries;
        Items = items;
        Skills = skills;
        Buffs = buffs;
        Assets = assets;
        Locales = locales;
    }

    public string RootDirectory { get; }

    public LongLiveModManifest Manifest { get; }

    public LongLiveStateKeyFile? StateKeys { get; }

    public LongLiveCommandFile? Commands { get; }

    public LongLiveQueryFile? Queries { get; }

    public LongLiveItemFile? Items { get; }

    public LongLiveSkillFile? Skills { get; }

    public LongLiveBuffFile? Buffs { get; }

    public LongLiveAssetMappingFile? Assets { get; }

    public IReadOnlyList<LongLiveLocaleResource> Locales { get; }
}
