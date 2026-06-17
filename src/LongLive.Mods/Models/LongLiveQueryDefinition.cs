using System.Text.Json;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveQueryDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("handler")]
    public string Handler { get; set; } = string.Empty;

    [JsonPropertyName("backend")]
    public string Backend { get; set; } = "builtin";

    [JsonPropertyName("options")]
    public JsonElement? Options { get; set; }
}
