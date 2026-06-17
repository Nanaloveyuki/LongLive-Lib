using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveModDependency
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
