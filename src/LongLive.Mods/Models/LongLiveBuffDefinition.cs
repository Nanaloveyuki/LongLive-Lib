using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveBuffDefinition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public int Icon { get; set; }

    [JsonPropertyName("buffType")]
    public int BuffType { get; set; }

    [JsonPropertyName("trigger")]
    public int Trigger { get; set; }

    [JsonPropertyName("removeTrigger")]
    public int RemoveTrigger { get; set; }

    [JsonPropertyName("seid")]
    public List<int> SeidList { get; set; } = new List<int>();

    [JsonPropertyName("affix")]
    public List<int> AffixList { get; set; } = new List<int>();

    [JsonPropertyName("hidden")]
    public bool Hidden { get; set; }
}
