using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveItemDefinition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("info")]
    public string Info { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public int Icon { get; set; }

    [JsonPropertyName("itemType")]
    public int ItemType { get; set; }

    [JsonPropertyName("quality")]
    public int Quality { get; set; }

    [JsonPropertyName("phase")]
    public int Phase { get; set; }

    [JsonPropertyName("maxStack")]
    public int MaxStack { get; set; } = 1;

    [JsonPropertyName("price")]
    public int Price { get; set; }

    [JsonPropertyName("seid")]
    public List<int> SeidList { get; set; } = new List<int>();

    [JsonPropertyName("affix")]
    public List<int> AffixList { get; set; } = new List<int>();

    [JsonPropertyName("flags")]
    public List<int> Flags { get; set; } = new List<int>();
}
