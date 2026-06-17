using System.Text.Json;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveStateKeyDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("default")]
    public JsonElement? DefaultValue { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
