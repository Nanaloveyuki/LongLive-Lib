using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveSkillFile
{
    [JsonPropertyName("skills")]
    public List<LongLiveSkillDefinition> Skills { get; set; } = new List<LongLiveSkillDefinition>();
}
