using System.Text.Json;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveCommandArgumentDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("default")]
    public JsonElement? DefaultValue { get; set; }
}
