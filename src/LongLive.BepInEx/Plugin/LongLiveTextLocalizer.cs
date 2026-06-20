using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using LongLive.Next.Runtime;

namespace LongLive.BepInEx.Plugin;

internal sealed class LongLiveTextLocalizer
{
    private const string DefaultLocale = "en-US";

    private const string ResourcePrefix = "LongLive.BepInEx.Localization.";

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> Catalog =
        new Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>(LoadCatalog);

    private readonly NextRuntimeFacade _runtime;

    public LongLiveTextLocalizer(NextRuntimeFacade runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public string Get(string key)
    {
        var localeTable = ResolveTable();
        if (localeTable.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var defaultTable = GetTable(DefaultLocale);
        if (defaultTable.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return key;
    }

    public string GetOrNa(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? Get("common.na") : value!;
    }

    private IReadOnlyDictionary<string, string> ResolveTable()
    {
        var languageDir = _runtime.IsAvailable ? _runtime.Localization.GetCurrentLanguageDirectory() : null;
        return GetTable(NormalizeLocale(languageDir));
    }

    private static IReadOnlyDictionary<string, string> GetTable(string locale)
    {
        if (Catalog.Value.TryGetValue(locale, out var table))
        {
            return table;
        }

        return Catalog.Value[DefaultLocale];
    }

    private static string NormalizeLocale(string? languageDir)
    {
        if (languageDir is null)
        {
            return DefaultLocale;
        }

        var normalized = languageDir.Trim();
        if (normalized.Length == 0)
        {
            return DefaultLocale;
        }

        if (normalized.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "cn", StringComparison.OrdinalIgnoreCase) ||
            normalized.IndexOf("中文", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("chinese", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "zh-CN";
        }

        return DefaultLocale;
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadCatalog()
    {
        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var assembly = typeof(LongLiveTextLocalizer).Assembly;

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                !resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var locale = ExtractLocale(resourceName);
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(StringComparer.Ordinal);
            result[locale] = new Dictionary<string, string>(entries, StringComparer.Ordinal);
        }

        if (!result.ContainsKey(DefaultLocale))
        {
            result[DefaultLocale] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["common.na"] = "n/a",
            };
        }

        return result;
    }

    private static string ExtractLocale(string resourceName)
    {
        return resourceName.Substring(ResourcePrefix.Length, resourceName.Length - ResourcePrefix.Length - ".json".Length);
    }
}
