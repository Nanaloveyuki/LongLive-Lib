using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveModManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("authors")]
    public List<string> Authors { get; set; } = new List<string>();

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("dependencies")]
    public List<LongLiveModDependency> Dependencies { get; set; } = new List<LongLiveModDependency>();

    [JsonPropertyName("entrypoints")]
    public LongLiveModEntrypoints Entrypoints { get; set; } = new LongLiveModEntrypoints();

    [JsonPropertyName("locales")]
    public List<string> Locales { get; set; } = new List<string>();
}
