using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveSkillDefinition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("skillPkId")]
    public int SkillPkId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("guideDescription")]
    public string GuideDescription { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public int Icon { get; set; }

    [JsonPropertyName("quality")]
    public int Quality { get; set; }

    [JsonPropertyName("phase")]
    public int Phase { get; set; }

    [JsonPropertyName("baseDamage")]
    public int BaseDamage { get; set; }

    [JsonPropertyName("attackScript")]
    public string AttackScript { get; set; } = string.Empty;

    [JsonPropertyName("battle")]
    public bool Battle { get; set; }

    [JsonPropertyName("learnLevel")]
    public int LearnLevel { get; set; }

    [JsonPropertyName("learnCostMonth")]
    public int LearnCostMonth { get; set; }

    [JsonPropertyName("seid")]
    public List<int> SeidList { get; set; } = new List<int>();

    [JsonPropertyName("affix")]
    public List<int> AffixList { get; set; } = new List<int>();

    [JsonPropertyName("costTypes")]
    public List<int> CostTypes { get; set; } = new List<int>();

    [JsonPropertyName("costValues")]
    public List<int> CostValues { get; set; } = new List<int>();
}
