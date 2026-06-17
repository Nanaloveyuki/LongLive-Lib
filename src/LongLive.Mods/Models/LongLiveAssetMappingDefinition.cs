using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveAssetMappingDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "replace";
}
