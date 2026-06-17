using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveBuffFile
{
    [JsonPropertyName("buffs")]
    public List<LongLiveBuffDefinition> Buffs { get; set; } = new List<LongLiveBuffDefinition>();
}
