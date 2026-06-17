using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveCommandDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("handler")]
    public string Handler { get; set; } = string.Empty;

    [JsonPropertyName("backend")]
    public string Backend { get; set; } = "builtin";

    [JsonPropertyName("args")]
    public List<LongLiveCommandArgumentDefinition> Arguments { get; set; } = new List<LongLiveCommandArgumentDefinition>();

    [JsonPropertyName("options")]
    public JsonElement? Options { get; set; }
}
