using System;
using System.Collections.Generic;
using System.Text.Json;
using LongLive.Mods.Models;

namespace LongLive.Mods.Validation;

public sealed class LongLiveModValidator
{
    private static readonly HashSet<string> SupportedBackends = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "builtin",
        "next-script",
    };

    private static readonly HashSet<string> SupportedArgumentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "int",
        "string",
        "bool",
        "float",
    };

    private static readonly HashSet<string> SupportedStateKeyTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "int",
        "string",
    };

    public LongLiveModValidationResult Validate(LongLiveModPackage package)
    {
        if (package is null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        var result = new LongLiveModValidationResult();

        ValidateManifest(package.Manifest, result);
        ValidateStateKeys(package.StateKeys, result);
        ValidateCommands(package.Commands, result);
        ValidateQueries(package.Queries, result);
        ValidateItems(package.Items, result);
        ValidateSkills(package.Skills, result);
        ValidateBuffs(package.Buffs, result);
        ValidateAssets(package.Assets, result);
        ValidateLocales(package.Locales, result);

        return result;
    }

    private static void ValidateManifest(LongLiveModManifest manifest, LongLiveModValidationResult result)
    {
        if (manifest.SchemaVersion < 1)
        {
            result.AddError("manifest.schema_version", "Manifest schemaVersion must be greater than or equal to 1.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            result.AddError("manifest.id", "Manifest id is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            result.AddError("manifest.name", "Manifest name is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            result.AddError("manifest.version", "Manifest version is required.");
        }

        foreach (var dependency in manifest.Dependencies)
        {
            if (string.IsNullOrWhiteSpace(dependency.Id))
            {
                result.AddError("manifest.dependencies.id", "Dependency id must not be empty.");
            }
        }

        if (manifest.Locales.Count == 0)
        {
            result.AddWarning("manifest.locales", "No locale files are declared in the manifest.");
        }
    }

    private static void ValidateStateKeys(LongLiveStateKeyFile? stateKeyFile, LongLiveModValidationResult result)
    {
        if (stateKeyFile is null)
        {
            return;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in stateKeyFile.Keys)
        {
            if (string.IsNullOrWhiteSpace(key.Id))
            {
                result.AddError("state_keys.id", "State key id must not be empty.");
            }
            else if (!ids.Add(key.Id))
            {
                result.AddError("state_keys.duplicate", $"Duplicate state key id: {key.Id}");
            }

            if (!SupportedStateKeyTypes.Contains(key.Type))
            {
                result.AddError("state_keys.type", $"Unsupported state key type: {key.Type}");
            }

            ValidateStateDefaultValue(key, result);
        }
    }

    private static void ValidateStateDefaultValue(LongLiveStateKeyDefinition key, LongLiveModValidationResult result)
    {
        if (key.DefaultValue is null)
        {
            return;
        }

        if (string.Equals(key.Type, "int", StringComparison.OrdinalIgnoreCase))
        {
            if (key.DefaultValue.Value.ValueKind != JsonValueKind.Number || !key.DefaultValue.Value.TryGetInt32(out _))
            {
                result.AddError("state_keys.default", $"State key default must be an int for key: {key.Id}");
            }

            return;
        }

        if (string.Equals(key.Type, "string", StringComparison.OrdinalIgnoreCase) &&
            key.DefaultValue.Value.ValueKind != JsonValueKind.String)
        {
            result.AddError("state_keys.default", $"State key default must be a string for key: {key.Id}");
        }
    }

    private static void ValidateCommands(LongLiveCommandFile? commandFile, LongLiveModValidationResult result)
    {
        if (commandFile is null)
        {
            return;
        }

        var commandIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var command in commandFile.Commands)
        {
            if (string.IsNullOrWhiteSpace(command.Id))
            {
                result.AddError("commands.id", "Command id must not be empty.");
            }
            else if (!commandIds.Add(command.Id))
            {
                result.AddError("commands.duplicate", $"Duplicate command id: {command.Id}");
            }

            if (string.IsNullOrWhiteSpace(command.Handler))
            {
                result.AddError("commands.handler", $"Command handler must not be empty for id: {command.Id}");
            }

            if (!SupportedBackends.Contains(command.Backend))
            {
                result.AddError("commands.backend", $"Unsupported command backend: {command.Backend}");
            }

            var indexes = new HashSet<int>();
            foreach (var argument in command.Arguments)
            {
                if (string.IsNullOrWhiteSpace(argument.Name))
                {
                    result.AddError("commands.args.name", $"Command argument name must not be empty for command: {command.Id}");
                }

                if (!indexes.Add(argument.Index))
                {
                    result.AddError("commands.args.index", $"Duplicate argument index {argument.Index} for command: {command.Id}");
                }

                if (!SupportedArgumentTypes.Contains(argument.Type))
                {
                    result.AddError("commands.args.type", $"Unsupported argument type {argument.Type} for command: {command.Id}");
                }
            }
        }
    }

    private static void ValidateQueries(LongLiveQueryFile? queryFile, LongLiveModValidationResult result)
    {
        if (queryFile is null)
        {
            return;
        }

        var queryIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var query in queryFile.Queries)
        {
            if (string.IsNullOrWhiteSpace(query.Id))
            {
                result.AddError("queries.id", "Query id must not be empty.");
            }
            else if (!queryIds.Add(query.Id))
            {
                result.AddError("queries.duplicate", $"Duplicate query id: {query.Id}");
            }

            if (string.IsNullOrWhiteSpace(query.Handler))
            {
                result.AddError("queries.handler", $"Query handler must not be empty for id: {query.Id}");
            }

            if (!SupportedBackends.Contains(query.Backend))
            {
                result.AddError("queries.backend", $"Unsupported query backend: {query.Backend}");
            }
        }
    }

    private static void ValidateLocales(IReadOnlyList<LongLiveLocaleResource> locales, LongLiveModValidationResult result)
    {
        var localePaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var locale in locales)
        {
            if (!localePaths.Add(locale.RelativePath))
            {
                result.AddError("locales.duplicate", $"Duplicate locale file: {locale.RelativePath}");
            }

            try
            {
                using var document = JsonDocument.Parse(locale.Content);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    result.AddError("locales.format", $"Locale file must contain a JSON object: {locale.RelativePath}");
                }
            }
            catch (JsonException)
            {
                result.AddError("locales.parse", $"Locale file is not valid JSON: {locale.RelativePath}");
            }
        }
    }

    private static void ValidateItems(LongLiveItemFile? itemFile, LongLiveModValidationResult result)
    {
        if (itemFile is null)
        {
            return;
        }

        var ids = new HashSet<int>();
        foreach (var item in itemFile.Items)
        {
            if (item.Id <= 0)
            {
                result.AddError("items.id", "Item id must be greater than zero.");
            }
            else if (!ids.Add(item.Id))
            {
                result.AddError("items.duplicate", $"Duplicate item id: {item.Id}");
            }

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                result.AddError("items.name", $"Item name is required for id: {item.Id}");
            }
        }
    }

    private static void ValidateSkills(LongLiveSkillFile? skillFile, LongLiveModValidationResult result)
    {
        if (skillFile is null)
        {
            return;
        }

        var ids = new HashSet<int>();
        foreach (var skill in skillFile.Skills)
        {
            if (skill.Id <= 0)
            {
                result.AddError("skills.id", "Skill id must be greater than zero.");
            }
            else if (!ids.Add(skill.Id))
            {
                result.AddError("skills.duplicate", $"Duplicate skill id: {skill.Id}");
            }

            if (string.IsNullOrWhiteSpace(skill.Name))
            {
                result.AddError("skills.name", $"Skill name is required for id: {skill.Id}");
            }

            if (string.IsNullOrWhiteSpace(skill.AttackScript))
            {
                result.AddError("skills.attack_script", $"Skill attackScript is required for id: {skill.Id}");
            }
        }
    }

    private static void ValidateBuffs(LongLiveBuffFile? buffFile, LongLiveModValidationResult result)
    {
        if (buffFile is null)
        {
            return;
        }

        var ids = new HashSet<int>();
        foreach (var buff in buffFile.Buffs)
        {
            if (buff.Id <= 0)
            {
                result.AddError("buffs.id", "Buff id must be greater than zero.");
            }
            else if (!ids.Add(buff.Id))
            {
                result.AddError("buffs.duplicate", $"Duplicate buff id: {buff.Id}");
            }

            if (string.IsNullOrWhiteSpace(buff.Name))
            {
                result.AddError("buffs.name", $"Buff name is required for id: {buff.Id}");
            }
        }
    }

    private static void ValidateAssets(LongLiveAssetMappingFile? assetFile, LongLiveModValidationResult result)
    {
        if (assetFile is null)
        {
            return;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var asset in assetFile.Assets)
        {
            if (string.IsNullOrWhiteSpace(asset.Id))
            {
                result.AddError("assets.id", "Asset mapping id must not be empty.");
            }
            else if (!ids.Add(asset.Id))
            {
                result.AddError("assets.duplicate", $"Duplicate asset mapping id: {asset.Id}");
            }

            if (string.IsNullOrWhiteSpace(asset.Kind))
            {
                result.AddError("assets.kind", $"Asset kind is required for mapping: {asset.Id}");
            }

            if (string.IsNullOrWhiteSpace(asset.Target))
            {
                result.AddError("assets.target", $"Asset target is required for mapping: {asset.Id}");
            }

            if (string.IsNullOrWhiteSpace(asset.Source))
            {
                result.AddError("assets.source", $"Asset source is required for mapping: {asset.Id}");
            }
        }
    }
}
