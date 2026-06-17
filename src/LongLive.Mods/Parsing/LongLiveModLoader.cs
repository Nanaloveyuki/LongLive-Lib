using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LongLive.Mods.Exceptions;
using LongLive.Mods.Models;

namespace LongLive.Mods.Parsing;

public sealed class LongLiveModLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false,
    };

    public LongLiveModPackage LoadFromDirectory(string modDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(modDirectoryPath))
        {
            throw new ArgumentException("Mod directory path must not be empty.", nameof(modDirectoryPath));
        }

        var rootDirectory = Path.GetFullPath(modDirectoryPath);
        if (!Directory.Exists(rootDirectory))
        {
            throw new DirectoryNotFoundException($"Mod directory not found: {rootDirectory}");
        }

        var manifestPath = Path.Combine(rootDirectory, "manifest.json");
        var manifest = ReadJsonFile<LongLiveModManifest>(manifestPath);

        var stateKeyPath = manifest.Entrypoints.StateKeys;
        var stateKeys = string.IsNullOrWhiteSpace(stateKeyPath)
            ? null
            : ReadJsonFile<LongLiveStateKeyFile>(ResolveRelativeFile(rootDirectory, stateKeyPath!));

        var commandPath = manifest.Entrypoints.Commands;
        var commands = string.IsNullOrWhiteSpace(commandPath)
            ? null
            : ReadJsonFile<LongLiveCommandFile>(ResolveRelativeFile(rootDirectory, commandPath!));

        var queryPath = manifest.Entrypoints.Queries;
        var queries = string.IsNullOrWhiteSpace(queryPath)
            ? null
            : ReadJsonFile<LongLiveQueryFile>(ResolveRelativeFile(rootDirectory, queryPath!));

        var itemPath = manifest.Entrypoints.Content.Items;
        var items = string.IsNullOrWhiteSpace(itemPath)
            ? null
            : ReadJsonFile<LongLiveItemFile>(ResolveRelativeFile(rootDirectory, itemPath!));

        var skillPath = manifest.Entrypoints.Content.Skills;
        var skills = string.IsNullOrWhiteSpace(skillPath)
            ? null
            : ReadJsonFile<LongLiveSkillFile>(ResolveRelativeFile(rootDirectory, skillPath!));

        var buffPath = manifest.Entrypoints.Content.Buffs;
        var buffs = string.IsNullOrWhiteSpace(buffPath)
            ? null
            : ReadJsonFile<LongLiveBuffFile>(ResolveRelativeFile(rootDirectory, buffPath!));

        var assetPath = manifest.Entrypoints.Content.Assets;
        var assets = string.IsNullOrWhiteSpace(assetPath)
            ? null
            : ReadJsonFile<LongLiveAssetMappingFile>(ResolveRelativeFile(rootDirectory, assetPath!));

        var locales = new List<LongLiveLocaleResource>();
        foreach (var localePath in manifest.Locales)
        {
            var resolvedPath = ResolveRelativeFile(rootDirectory, localePath);
            locales.Add(new LongLiveLocaleResource(localePath, File.ReadAllText(resolvedPath)));
        }

        return new LongLiveModPackage(rootDirectory, manifest, stateKeys, commands, queries, items, skills, buffs, assets, locales);
    }

    private static T ReadJsonFile<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Required mod file not found: {filePath}", filePath);
        }

        try
        {
            var content = File.ReadAllText(filePath);
            var result = JsonSerializer.Deserialize<T>(content, SerializerOptions);
            if (result is null)
            {
                throw new LongLiveModLoadException($"JSON file deserialized to null: {filePath}");
            }

            return result;
        }
        catch (JsonException exception)
        {
            throw new LongLiveModLoadException($"Failed to parse JSON file: {filePath}", exception);
        }
    }

    private static string ResolveRelativeFile(string rootDirectory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new LongLiveModLoadException("Relative mod file path must not be empty.");
        }

        var fullRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(rootDirectory));
        var fullPath = Path.GetFullPath(Path.Combine(rootDirectory, relativePath));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new LongLiveModLoadException($"Resolved mod path escapes the package root: {relativePath}");
        }

        return fullPath;
    }

    private static string EnsureTrailingDirectorySeparator(string directoryPath)
    {
        if (directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
            directoryPath.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            return directoryPath;
        }

        return directoryPath + Path.DirectorySeparatorChar;
    }
}
